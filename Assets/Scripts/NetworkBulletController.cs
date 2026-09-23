// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Netcode;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkBulletController : NetworkBehaviour
    {
        [Tooltip("The initial force applied to the bullet.")]
        [SerializeField] private float m_Speed = 300f;

        [Tooltip("Seconds before the server despawns the bullet, so bullets don't accumulate.")]
        [SerializeField] private float m_Lifetime = 5f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                Invoke(nameof(DespawnSelf), m_Lifetime);
        }

        private void DespawnSelf()
        {
            if (IsSpawned)
                NetworkObject.Despawn();
        }

        private void Start()
        {
            // Apply the initial force to the bullet
            GetComponent<Rigidbody>().AddForce(transform.forward * m_Speed);
        }
    }
}
