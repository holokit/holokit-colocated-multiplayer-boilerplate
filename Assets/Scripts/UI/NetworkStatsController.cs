// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class NetworkStatsController : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NetworkStatusText;

        [SerializeField] private TMP_Text m_PingText;

        [SerializeField] private TMP_Text m_ConnectedDeviceCountText;

        public void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            m_ConnectedDeviceCountText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                m_ConnectedDeviceCountText.text = $"Connected Device Count: {NetworkManager.Singleton.ConnectedClients.Count}";
        }

        public void OnHostStarted()
        {
            m_NetworkStatusText.text = "Network Status: Hosting";
            m_ConnectedDeviceCountText.text = $"Connected Device Count: {NetworkManager.Singleton.ConnectedClients.Count}";
            m_ConnectedDeviceCountText.gameObject.SetActive(true);
        }

        public void OnClientStarted()
        {
            m_NetworkStatusText.text = "Network Status: Connecting";
        }

        public void OnShutdown()
        {
            m_NetworkStatusText.text = "Network Status: None";
            m_PingText.text = "0ms";
            m_ConnectedDeviceCountText.gameObject.SetActive(false);
        }

        public void OnVisibilityChanged(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void OnReceivedRtt(int rtt)
        {
            m_PingText.text = $"Ping: {rtt}ms";
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                m_NetworkStatusText.text = "Network Status: Connected";
        }

        private void OnClientDisconnect(ulong clientId)
        {

        }
    }
}
