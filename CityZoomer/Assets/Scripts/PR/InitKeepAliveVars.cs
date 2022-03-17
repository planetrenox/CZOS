using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PR
{
    public class InitKeepAliveVars : MonoBehaviour
    {
        private Dictionary<CSteamID, Transform> playerTransforms;
        private UnsafeSteamNetworkingP2P unsafeSteamNetworkingP2P;
        private Transform[] Transforms_Cars;
        private GameObject[] GameObjects_Cars;
        int currentCar, carCount;


        private void Awake()
        {
            unsafeSteamNetworkingP2P = GameObject.Find("StaysAlive").GetComponent<UnsafeSteamNetworkingP2P>();
            unsafeSteamNetworkingP2P.selfPlayerTransform = GameObject.Find("CameraSight").GetComponent<Transform>();
            GameObjects_Cars = GameObject.FindGameObjectsWithTag("Car");
            carCount = GameObjects_Cars.Length;
            Transforms_Cars = new Transform[GameObjects_Cars.Length];
            for (int i = 0; i < GameObjects_Cars.Length; i++)
            {
                Transforms_Cars[i] = GameObjects_Cars[i].transform;
            }
            currentCar = 0;

            
            generateNewModels();
        }

        private void Update()
        {
            if (unsafeSteamNetworkingP2P.availablePlayerModelsInScene < PR.SteamLobby.sArrSteamID_LobbyMembers.Length - 1) generateNewModels();

            CullCar();
            CullCar();
            
        }

        void CullCar()
        {
            var state = GameObjects_Cars[currentCar].activeSelf;
            var distanceToPlayer = Vector3.Distance(Transforms_Cars[currentCar].position, unsafeSteamNetworkingP2P.selfPlayerTransform.position);
            if (distanceToPlayer > 230)
            {
                if (state) GameObjects_Cars[currentCar].SetActive(false);
            }
            else if (!state)
            {
                if (distanceToPlayer > 60) GameObjects_Cars[currentCar].SetActive(true);
            }

            currentCar++;
            if (currentCar >= carCount) currentCar = 0;
        }

        private void generateNewModels()
        {
            if (PR.SteamLobby.sArrSteamID_LobbyMembers.Length <= 1) return;
            var kiki = GameObject.Find("Kiki-v2");
            var availablePlayerModelsInScene = 0;
            playerTransforms = new Dictionary<CSteamID, Transform>();
            foreach (var t in PR.SteamLobby.sArrSteamID_LobbyMembers)
            {
                if (t == unsafeSteamNetworkingP2P.selfID) continue;
                var newKiki = Instantiate(kiki);
                availablePlayerModelsInScene++;
                playerTransforms.Add(t, newKiki.GetComponent<Transform>());
            }

            unsafeSteamNetworkingP2P.playerTransforms = playerTransforms;
            unsafeSteamNetworkingP2P.availablePlayerModelsInScene = availablePlayerModelsInScene;
        }
    }
}