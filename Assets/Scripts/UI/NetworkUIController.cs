// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using HoloKit;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class NetworkUIController : MonoBehaviour
    {
        [SerializeField] GameObject m_StartHostButton;

        [SerializeField] GameObject m_StartClientButton;

        [SerializeField] GameObject m_ShutdownButton;

        [SerializeField] GameObject m_FireButton;

        public UnityEvent OnBeforeHostStarted;

        public UnityEvent OnHostStarted;

        public UnityEvent OnClientStarted;

        public UnityEvent OnShutdown;

        public UnityEvent<bool> OnVisibilityChanged;

        private HoloKitCameraManager m_HoloKitCameraManager;

        private void Start()
        {
            m_HoloKitCameraManager = FindFirstObjectByType<HoloKitCameraManager>();
            if (m_HoloKitCameraManager != null)
                m_HoloKitCameraManager.OnScreenRenderModeChanged += OnScreenRenderModeChanged;
            // Covers the host leaving, a rejected connection, or a failed connection attempt.
            NetworkManager.Singleton.OnClientStopped += OnNetworkStopped;
        }

        private void OnDestroy()
        {
            if (m_HoloKitCameraManager != null)
                m_HoloKitCameraManager.OnScreenRenderModeChanged -= OnScreenRenderModeChanged;
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientStopped -= OnNetworkStopped;
        }

        private void OnScreenRenderModeChanged(ScreenRenderMode renderMode)
        {
            gameObject.SetActive(renderMode == ScreenRenderMode.Mono);
            OnVisibilityChanged?.Invoke(renderMode == ScreenRenderMode.Mono);
        }

        private void Update()
        {
            m_FireButton.SetActive(NetworkManager.Singleton.IsConnectedClient);
        }

        public void StartHost()
        {
            OnBeforeHostStarted?.Invoke();
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[NetworkUIController] Failed to start host");
                return;
            }
            OnHostStarted?.Invoke();
            ShowSessionButtons(true);
        }

        public void StartClient()
        {
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[NetworkUIController] Failed to start client");
                return;
            }
            OnClientStarted?.Invoke();
            ShowSessionButtons(true);
        }

        public void Shutdown()
        {
            NetworkManager.Singleton.Shutdown();
            ResetUI();
        }

        private void OnNetworkStopped(bool wasHost)
        {
            if (m_ShutdownButton.activeSelf)
                ResetUI();
        }

        private void ResetUI()
        {
            OnShutdown?.Invoke();
            ShowSessionButtons(false);
        }

        private void ShowSessionButtons(bool inSession)
        {
            m_StartHostButton.SetActive(!inSession);
            m_StartClientButton.SetActive(!inSession);
            m_ShutdownButton.SetActive(inSession);
        }
    }
}
