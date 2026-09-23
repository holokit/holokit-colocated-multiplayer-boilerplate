// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-FileContributor: Botao Amber Hu <botao.a.hu@gmail.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Netcode;
using Immersal.XR;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    /// <summary>
    /// We use this script to synchronize device pose with Immersal SDK.
    /// Immersal SDK moves the ARSpace instead of the camera to achieve AR map relocalization.
    /// Thus, we need to calculate the player pose according to the ARSpace pose in the reverse order.
    /// </summary>
    [RequireComponent(typeof(HoloKitMarkManager))]
    public class PlayerPoseSynchronizer_Immersal : NetworkBehaviour
    {
        [SerializeField] private float m_PositionLerpSpeed = 5f;

        [SerializeField] private float m_RotationLerpSpeed = 5f;

        private XRSpace m_XRSpace;

        private Transform m_CenterEyePose;

        private NetworkVariable<Vector3> m_RelativePosition = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<Quaternion> m_RelativeRotation = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                //transform.SetParent(FindFirstObjectByType<ARMap>().transform);
                m_CenterEyePose = FindFirstObjectByType<HoloKitCameraManager>().CenterEyePose;
            }
            m_XRSpace = FindFirstObjectByType<XRSpace>();
        }

        private void Update()
        {
            // Not spawned yet, or the scene has no Immersal space or HoloKit camera.
            if (!IsSpawned || m_XRSpace == null)
                return;
            if (IsOwner && m_CenterEyePose != null)
            {
                m_RelativePosition.Value = m_XRSpace.transform.InverseTransformPoint(m_CenterEyePose.position);
                m_RelativeRotation.Value = Quaternion.Inverse(m_XRSpace.transform.rotation) * m_CenterEyePose.rotation;
            }
        }

        private void LateUpdate()
        {
            if (!IsSpawned || m_XRSpace == null)
                return;
            Vector3 targetPosition = m_XRSpace.transform.TransformPoint(m_RelativePosition.Value);
            Quaternion targetRotation = m_XRSpace.transform.rotation * m_RelativeRotation.Value;

            // Interpolate the position
            transform.position = Vector3.Lerp(transform.position, targetPosition, m_PositionLerpSpeed * Time.deltaTime);
            // Interpolate the rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_RotationLerpSpeed * Time.deltaTime);
        }
    }
}
