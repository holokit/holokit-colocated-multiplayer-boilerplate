// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Netcode;
using Netcode.Transports.NetworkFramework;
using Unity.Netcode.Transports.UTP;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public enum AvailableTransport
    {
        /// <summary>
        /// Nearby devices connect directly over Apple peer-to-peer Wi-Fi (Network framework),
        /// no router needed. Replaces the Multipeer Connectivity ("AirDrop") transport.
        /// </summary>
        [InspectorName("Nearby (Network framework)")]
        Nearby = 0,
        Router = 1
    }

    public class TransportSelector : MonoBehaviour
    {
        [FormerlySerializedAs("m_AirDropTransport")]
        [SerializeField] private NetworkFrameworkTransport m_NearbyTransport;

        [SerializeField] private UnityTransport m_UnityTransport;

        [FormerlySerializedAs("m_AirDropToggle")]
        [SerializeField] private Toggle m_NearbyToggle;

        [SerializeField] private Toggle m_RouterToggle;

        [SerializeField] private AvailableTransport m_DefaultTransport = AvailableTransport.Nearby;

        public AvailableTransport CurrentTransport => m_CurrentTransport;

        private bool m_IsToggling = false;

        private AvailableTransport m_CurrentTransport;

        private void Start()
        {
            // Peer-to-peer needs iOS 26+; fall back to the router transport elsewhere.
            if (m_DefaultTransport == AvailableTransport.Router || !NetworkFrameworkTransport.IsPlatformSupported)
                OnRouterToggled(true);
            else
                OnNearbyToggled(true);
        }

        public void OnNearbyToggled(bool value)
        {
            Select(AvailableTransport.Nearby, value);
        }

        public void OnRouterToggled(bool value)
        {
            Select(AvailableTransport.Router, value);
        }

        /// <summary>Kept for scenes whose toggle still calls the old method name.</summary>
        public void OnAirDropToggled(bool value) => OnNearbyToggled(value);

        private void Select(AvailableTransport transport, bool value)
        {
            if (m_IsToggling) return;
            m_IsToggling = true;

            // The toggles act as radio buttons: turning the active one off turns it back on.
            if (!value && transport != m_CurrentTransport)
            {
                m_IsToggling = false;
                return;
            }
            NetworkManager.Singleton.NetworkConfig.NetworkTransport =
                transport == AvailableTransport.Nearby ? m_NearbyTransport : m_UnityTransport;
            m_CurrentTransport = transport;
            m_NearbyToggle.isOn = transport == AvailableTransport.Nearby;
            m_RouterToggle.isOn = transport == AvailableTransport.Router;

            m_IsToggling = false;
        }
    }
}
