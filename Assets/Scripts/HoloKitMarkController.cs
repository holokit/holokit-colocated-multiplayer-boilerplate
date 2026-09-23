// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using HoloKit;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class HoloKitMarkController : MonoBehaviour
    {
        public Transform PlayerPoseSynchronizer { get; set; }

        [SerializeField] private Vector3 m_Offset = new(0f, 0.15f, 0f);

        private Transform m_CenterEyePose;

        private void Start()
        {
            m_CenterEyePose = FindFirstObjectByType<HoloKitCameraManager>().CenterEyePose;
        }

        private void LateUpdate()
        {
            transform.position = PlayerPoseSynchronizer.position + m_Offset;
            transform.rotation = Quaternion.Euler(0f, m_CenterEyePose.rotation.eulerAngles.y, 0f);
        }
    }
}
