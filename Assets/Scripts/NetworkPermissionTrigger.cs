// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public class NetworkPermissionTrigger : MonoBehaviour
    {
        // URL to trigger network permission - it should be a valid URL
        private string testUrl = "https://apple.com";

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(RequestNetworkPermission());
        }

        IEnumerator RequestNetworkPermission()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(testUrl))
            {
                // Send the request and wait for a response
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Error requesting network permission: {webRequest.error}");
                }
                else
                {
                    Debug.Log("Network permission has been triggered successfully.");
                }
            }
        }
    }
}
