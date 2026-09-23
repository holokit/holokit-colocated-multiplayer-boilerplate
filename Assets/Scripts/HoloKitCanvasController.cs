// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using UnityEngine;
using HoloKit;

/// <summary>
/// This script is responsible for deactivating canvases when switching to stereo mode.
/// </summary>
namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class HoloKitCanvasController : MonoBehaviour
    {
        [SerializeField] List<GameObject> m_Canvases;

        private void Start()
        {
            FindFirstObjectByType<HoloKitCameraManager>().OnScreenRenderModeChanged += OnScreenRenderModeChanged;
        }

        private void OnScreenRenderModeChanged(ScreenRenderMode renderMode)
        {
            foreach (var canvas in m_Canvases)
            {
                canvas.SetActive(renderMode != ScreenRenderMode.Stereo);
            }

            if (renderMode == ScreenRenderMode.Mono)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
        }

        public void OnChangeScreenOrientation()
        {
            if (Screen.orientation == ScreenOrientation.Portrait)
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            else
                Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
