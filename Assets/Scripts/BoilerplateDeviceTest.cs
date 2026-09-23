// SPDX-FileCopyrightText: Copyright 2026 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Botao Amber Hu <botao@reality.design>
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HoloKit.ImageTrackingRelocalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    /// <summary>
    /// Unattended device test. Does nothing unless the app is launched with the environment
    /// variable BOILERPLATE_AUTOSTART=host|client (e.g. `devicectl device process launch -e`).
    /// It presses Host or Join like a player, lets the client fire bullets and ping, and logs
    /// "BOILERPLATE-TEST-RESULT PASS|FAIL" after BOILERPLATE_SECONDS (default 30).
    /// With BOILERPLATE_RELOCALIZE=1 it first relocalizes on the external marker and measures
    /// where the marker ends up in the shared frame ("BOILERPLATE-ALIGN"): ideally at the origin.
    /// </summary>
    public class BoilerplateDeviceTest : MonoBehaviour
    {
        private string m_Role;
        private int m_Seconds;
        private readonly List<int> m_Rtts = new();
        private readonly HashSet<ulong> m_BulletsSeen = new();
        private int m_MaxPlayersSeen;
        private int m_Disconnects;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string role = Environment.GetEnvironmentVariable("BOILERPLATE_AUTOSTART");
            if (role != "host" && role != "client")
                return;
            var test = new GameObject("BoilerplateDeviceTest").AddComponent<BoilerplateDeviceTest>();
            test.m_Role = role;
            test.m_Seconds = int.TryParse(Environment.GetEnvironmentVariable("BOILERPLATE_SECONDS"), out int s) ? s : 30;
            DontDestroyOnLoad(test.gameObject);
        }

        private static void Log(string line) => Debug.Log("BOILERPLATE-TEST " + line);

        private IEnumerator Start()
        {
            Log($"role={m_Role} device={SystemInfo.deviceModel} os={SystemInfo.operatingSystem} seconds={m_Seconds}");
            // Let the scene's own Start methods run first.
            yield return new WaitForSeconds(2f);
            var ui = FindFirstObjectByType<NetworkUIController>(FindObjectsInactive.Include);
            var manager = NetworkManager.Singleton;
            if (ui == null || manager == null)
            {
                Finish(false, "NetworkUIController or NetworkManager missing");
                yield break;
            }
            Log($"transport={manager.NetworkConfig.NetworkTransport.GetType().Name}");
            manager.OnClientDisconnectCallback += id =>
            {
                m_Disconnects++;
                Log($"disconnect client={id} reason={manager.DisconnectReason}");
            };
            var ping = FindFirstObjectByType<PingManager>(FindObjectsInactive.Include);
            if (ping != null)
                ping.OnReceivedRtt.AddListener(rtt => m_Rtts.Add(rtt));

            if (Environment.GetEnvironmentVariable("BOILERPLATE_RELOCALIZE") == "1")
                yield return Relocalize();

            if (m_Role == "host") ui.StartHost(); else ui.StartClient();

            float connectDeadline = Time.realtimeSinceStartup + 120f;
            while (m_Role == "host" ? manager.ConnectedClientsIds.Count < 2 : !manager.IsConnectedClient)
            {
                if (Time.realtimeSinceStartup > connectDeadline) { Finish(false, "no connection within 120 s"); yield break; }
                yield return null;
            }
            Log($"connected after {Time.realtimeSinceStartup:0.0}s since launch");

            var bullets = FindFirstObjectByType<NetworkBulletManager>(FindObjectsInactive.Include);
            float end = Time.realtimeSinceStartup + m_Seconds;
            float nextFire = 0, nextReport = 0;
            while (Time.realtimeSinceStartup < end)
            {
                Observe(manager);
                if (m_Role == "client" && bullets != null && Time.realtimeSinceStartup > nextFire)
                {
                    bullets.SpawnBullet();
                    nextFire = Time.realtimeSinceStartup + 1f;
                }
                if (Time.realtimeSinceStartup > nextReport)
                {
                    Log(Report(manager));
                    nextReport = Time.realtimeSinceStartup + 5f;
                }
                yield return null;
            }

            string summary = Report(manager);
            bool passed = m_Disconnects == 0 && m_MaxPlayersSeen >= 2 && m_BulletsSeen.Count > 0
                          && (m_Role == "host" || m_Rtts.Count >= m_Seconds / 2);
            Finish(passed, summary);
        }

        /// <summary>Yaw of a marker pose, using the same convention as TrackedImagePoseTransformer.</summary>
        private static float MarkerYaw(Quaternion rotation)
        {
            var r = Matrix4x4.Rotate(rotation * Quaternion.Euler(90f, 0f, 0f));
            return Mathf.Atan2(-r.m20 + r.m02, r.m00 + r.m22) * Mathf.Rad2Deg;
        }

        private IEnumerator Relocalize()
        {
            var stablizer = FindFirstObjectByType<ImageTrackingStablizer>(FindObjectsInactive.Include);
            var images = FindFirstObjectByType<ARTrackedImageManager>(FindObjectsInactive.Include);
            if (stablizer == null || images == null)
            {
                Log("ALIGN skipped: no ImageTrackingStablizer/ARTrackedImageManager in this scene");
                yield break;
            }
            bool locked = false;
            stablizer.OnTrackedImagePoseStablized.AddListener((_, _) => locked = true);
            float started = Time.realtimeSinceStartup;
            stablizer.IsRelocalizing = true;
            Log("ALIGN relocalizing: point the camera at the marker");
            float nextStatus = 0;
            while (!locked)
            {
                if (Time.realtimeSinceStartup > nextStatus)
                {
                    nextStatus = Time.realtimeSinceStartup + 3f;
                    var seen = new List<string>();
                    foreach (var i in images.trackables)
                        seen.Add($"{i.referenceImage.name}:{i.trackingState}@{i.transform.position.magnitude:0.00}m");
                    Log($"ALIGN status enabled={images.enabled} subsystem={images.subsystem?.running} " +
                        $"library={images.referenceLibrary?.count ?? -1} maxMoving={images.requestedMaxNumberOfMovingImages} " +
                        $"seen=[{string.Join(",", seen)}]");
                }
                if (Time.realtimeSinceStartup - started > 90f)
                {
                    stablizer.IsRelocalizing = false;
                    Log("ALIGN {\"locked\":false}");
                    yield break;
                }
                yield return null;
            }
            float lockSeconds = Time.realtimeSinceStartup - started;
            // The world origin reset is applied by ARKit asynchronously; let it settle.
            yield return new WaitForSeconds(1.5f);

            var positions = new List<Vector3>();
            var yaws = new List<float>();
            float sampleEnd = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < sampleEnd)
            {
                foreach (var image in images.trackables)
                {
                    if (image.trackingState != TrackingState.Tracking) continue;
                    positions.Add(image.transform.position);
                    yaws.Add(Mathf.DeltaAngle(0f, MarkerYaw(image.transform.rotation)));
                }
                yield return null;
            }
            if (positions.Count == 0)
            {
                Log($"ALIGN {{\"locked\":true,\"lockSeconds\":{lockSeconds:0.00},\"samples\":0}}");
                yield break;
            }
            Vector3 mean = positions.Aggregate(Vector3.zero, (a, b) => a + b) / positions.Count;
            float std = Mathf.Sqrt(positions.Average(p => (p - mean).sqrMagnitude));
            float maxMm = positions.Max(p => p.magnitude) * 1000f;
            float yawMean = yaws.Average();
            float yawStd = Mathf.Sqrt(yaws.Average(y => (y - yawMean) * (y - yawMean)));
            Log("ALIGN {" + $"\"locked\":true,\"lockSeconds\":{lockSeconds:0.00},\"samples\":{positions.Count}," +
                $"\"meanXmm\":{mean.x * 1000:0.0},\"meanYmm\":{mean.y * 1000:0.0},\"meanZmm\":{mean.z * 1000:0.0}," +
                $"\"offsetMm\":{mean.magnitude * 1000:0.0},\"stdMm\":{std * 1000:0.0},\"maxMm\":{maxMm:0.0}," +
                $"\"yawDeg\":{yawMean:0.00},\"yawStdDeg\":{yawStd:0.00}" + "}");
        }

        private void Observe(NetworkManager manager)
        {
            if (!manager.IsListening) return;
            int players = 0;
            foreach (var obj in manager.SpawnManager.SpawnedObjectsList)
            {
                if (obj.IsPlayerObject) players++;
                if (obj.TryGetComponent<NetworkBulletController>(out _)) m_BulletsSeen.Add(obj.NetworkObjectId);
            }
            m_MaxPlayersSeen = Math.Max(m_MaxPlayersSeen, players);
        }

        private string Report(NetworkManager manager)
        {
            var sorted = m_Rtts.OrderBy(r => r).ToList();
            int P(double p) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)(p * sorted.Count))];
            return $"players={m_MaxPlayersSeen} bullets={m_BulletsSeen.Count} rttSamples={sorted.Count} " +
                   $"rttP50={P(0.5)}ms rttP95={P(0.95)}ms rttMax={(sorted.Count == 0 ? 0 : sorted[^1])}ms " +
                   $"disconnects={m_Disconnects} fps={1f / Mathf.Max(0.0001f, Time.smoothDeltaTime):0}";
        }

        private void Finish(bool passed, string detail)
        {
            Debug.Log($"BOILERPLATE-TEST-RESULT {(passed ? "PASS" : "FAIL")} role={m_Role} {detail}");
        }
    }
}
