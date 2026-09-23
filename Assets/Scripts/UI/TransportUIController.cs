// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class TransportUIController : MonoBehaviour
    {
        [SerializeField] TransportSelector m_TransportSelector;

        [SerializeField] TMP_Text m_HostIPAddress;

        [SerializeField] GameObject m_IPInputField;

        private void Update()
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                m_IPInputField.SetActive(false);
                m_HostIPAddress.gameObject.SetActive(NetworkManager.Singleton.IsHost && m_TransportSelector.CurrentTransport == AvailableTransport.Router);
            }
            else
            {
                m_IPInputField.SetActive(m_TransportSelector.CurrentTransport == AvailableTransport.Router);
                m_HostIPAddress.gameObject.SetActive(false);
            }
        }

        public void OnBeforeHostStarted()
        {
            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (unityTransport != null)
            {
                string localIPAddress = GetLocalIPAddress();
                unityTransport.SetConnectionData(localIPAddress, (ushort)7777);
                unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
                m_HostIPAddress.text = $"Host IP Address: {localIPAddress}";
            }
        }

        public void OnHostStarted()
        {
            m_TransportSelector.gameObject.SetActive(false);
        }

        public void OnClientStarted()
        {
            m_TransportSelector.gameObject.SetActive(false);
        }

        public void OnShutdown()
        {
            m_TransportSelector.gameObject.SetActive(true);
        }

        public void OnVisibilityChanged(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// The IPv4 address other devices on the same Wi-Fi can reach, preferring Wi-Fi (en0).
        /// Returns "Unavailable" instead of throwing when the device has no network.
        /// </summary>
        public string GetLocalIPAddress()
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderBy(nic => nic.Name == "en0" ? 0 : 1)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Select(address => address.Address)
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip));
            var first = candidates.FirstOrDefault();
            return first != null ? first.ToString() : "Unavailable";
        }
    }
}
