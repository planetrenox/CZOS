using System;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

namespace PR
{
    public class SteamLobby : MonoBehaviour
    {
        public static CSteamID sSteamID_Lobby;
        public static CSteamID[] sArrSteamID_LobbyMembers = Array.Empty<CSteamID>();
        public static bool sBool_IsInLobby;
        private static Text sText_TopRight;
        private const int MaxLobbySize = 64;

        private void Awake()
        {
            sText_TopRight = GameObject.Find("Text_TopRight").GetComponent<Text>();
        }

        private void Start()
        {
            Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

            // check if game started via friend invitation & join if so
            if (Array.IndexOf(System.Environment.GetCommandLineArgs(), "+connect_lobby") is var pos && pos > -1 && ulong.TryParse(System.Environment.GetCommandLineArgs()[pos + 1], out var lobbyID))
                SteamMatchmaking.JoinLobby((CSteamID) lobbyID);
        }


        public static void RefreshLobbyMembers()
        {
            var numPlayers = SteamMatchmaking.GetNumLobbyMembers(sSteamID_Lobby);
            if (numPlayers == 0) return;
            sText_TopRight.text = "";
            CSteamID[] membersInLobby = new CSteamID[numPlayers];
            for (int i = 0; i < numPlayers; i++)
            {
                var lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(sSteamID_Lobby, i);
                membersInLobby[i] = lobbyMember;
                sText_TopRight.text += SteamFriends.GetFriendPersonaName(lobbyMember) + " –\n";
            }
            sArrSteamID_LobbyMembers = membersInLobby;
        }

        public void CreateLobby()
        {
            if (sBool_IsInLobby) SteamFriends.ActivateGameOverlayInviteDialog(sSteamID_Lobby);
            else SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxLobbySize);
        }

        public void LeaveLobby()
        {
            if (!sBool_IsInLobby) return;
            sBool_IsInLobby = false;
            sArrSteamID_LobbyMembers = Array.Empty<CSteamID>();
            SteamMatchmaking.LeaveLobby(sSteamID_Lobby);
        }


        private static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t gameLobbyJoinRequestedT) //accepted steam invite of someone else
        {
            SteamMatchmaking.JoinLobby(gameLobbyJoinRequestedT.m_steamIDLobby);
        }

        private static void OnLobbyCreated(LobbyCreated_t result)
        {
            if (result.m_eResult == EResult.k_EResultOK) SteamFriends.ActivateGameOverlayInviteDialog((CSteamID) result.m_ulSteamIDLobby);
            else Debug.Log("Lobby created -- failure ...");
        }


        private static void OnLobbyEnter(LobbyEnter_t result) // also called when our own lobby is created
        {
            if (result.m_EChatRoomEnterResponse == 1) // Success. https://partner.steamgames.com/doc/api/steam_api#EChatRoomEnterResponse
            {
                sSteamID_Lobby = (CSteamID) result.m_ulSteamIDLobby;
                RefreshLobbyMembers();
                sBool_IsInLobby = true;
            }
            else Debug.Log("Failed to join lobby.");
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t param) // Happens when OTHER users leave or join
        {
            RefreshLobbyMembers();
        }
    }
}