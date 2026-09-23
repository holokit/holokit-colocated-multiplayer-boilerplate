// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-FileContributor: Botao Amber Hu <botao.a.hu@gmail.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Netcode;
using HoloKit;
using Immersal.XR;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public enum RelocalizationMode
    {
        ImageTrackingRelocalizatioin = 0,
        Immersal = 1
    }

    public class NetworkBulletManager : NetworkBehaviour
    {
        [SerializeField] private RelocalizationMode m_RelocalizationMode;

        [SerializeField] private NetworkBulletController m_BulletPrefab;

        [SerializeField] private Vector3 m_SpawnOffset = new(0f, 0f, 0.3f);

        private Transform m_CenterEyePose;

        private XRSpace m_XRSpace;

        private void Start()
        {
            m_CenterEyePose = FindFirstObjectByType<HoloKitCameraManager>().CenterEyePose;

            if (m_RelocalizationMode == RelocalizationMode.Immersal)
            {
                m_XRSpace = FindFirstObjectByType<XRSpace>();
            }
        }

        public void SpawnBullet()
        {
            SpawnBulletServerRpc(m_CenterEyePose.position, m_CenterEyePose.rotation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SpawnBulletServerRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
        {
            if (m_RelocalizationMode == RelocalizationMode.ImageTrackingRelocalizatioin)
            {
                var bullet = Instantiate(m_BulletPrefab, position + rotation * m_SpawnOffset, rotation);
                bullet.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
                return;
            }

            if (m_RelocalizationMode == RelocalizationMode.Immersal)
            {
                Vector3 absolutePosition = position + rotation * m_SpawnOffset;
                Quaternion absoluteRotation = rotation;
                Vector3 relativePosition = m_XRSpace.transform.InverseTransformPoint(absolutePosition);
                Quaternion relativeRotation = Quaternion.Inverse(m_XRSpace.transform.rotation) * absoluteRotation;

                var bullet = Instantiate(m_BulletPrefab, m_XRSpace.transform);
                bullet.transform.localPosition = relativePosition;
                bullet.transform.localRotation = relativeRotation;
                bullet.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
                return;
            }
        }
    }
}
