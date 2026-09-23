// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class PingManager : NetworkBehaviour
    {
        [SerializeField] private float m_Interval = 1f;

        public UnityEvent<int> OnReceivedRtt;

        public override void OnNetworkSpawn()
        {
            if (!IsHost)
            {
                StartCoroutine(StartPing());
            }
        }

        private IEnumerator StartPing()
        {
            while (true)
            {
                PingServerRpc(Time.time);
                yield return new WaitForSeconds(m_Interval);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void PingServerRpc(float timestamp, RpcParams rpcParams = default)
        {
            // Answer only the client that pinged.
            PongClientRpc(timestamp, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PongClientRpc(float timestamp, RpcParams rpcParams)
        {
            int rtt = Mathf.FloorToInt((Time.time - timestamp) * 1000f);
            OnReceivedRtt?.Invoke(rtt);
        }
    }
}
