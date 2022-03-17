using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;


namespace PR
{
    public class UnsafeSteamNetworkingP2P : MonoBehaviour
    {
        protected Callback<P2PSessionRequest_t> Callback_newConnection;

        [NonSerialized]public Transform selfPlayerTransform;
        [NonSerialized]public CSteamID selfID;
        [NonSerialized]public Dictionary<CSteamID, Transform> playerTransforms;
        [NonSerialized]public int availablePlayerModelsInScene;


        void Start()
        {
            selfID = SteamUser.GetSteamID();
            Callback_newConnection = Callback<P2PSessionRequest_t>.Create(OnNewConnection);
        }

        private Vector3 prevPos = Vector3.zero;


        // TYPE 1 = SENDING OR RECEIVING POSITION
        // TYPE 2 = SENDING OR RECEIVING ROTATION
        // TYPE 3 = KEEP ALIVE WHILE 1 OR BOTH USERS ARE IN MENU
        private void Update()
        {
            switch (PR.SteamLobby.sBool_IsInLobby)
            {
                case true when PR.UI.isInStandaloneMenu:
                    net_broadcast(3, keepAlive());
                    getNetworkData();
                    break;
                case true when !PR.UI.isInStandaloneMenu:
                    net_broadcast(1, preparePosition());
                    net_broadcast(2, prepareRotation());
                    getNetworkData();
                    break;
            }
        }

        private byte[] preparePosition()
        {
            return vector3ToByte(selfPlayerTransform.position);
        }

        private byte[] prepareRotation()
        {
            return vector3ToByte(selfPlayerTransform.rotation.eulerAngles);
        }

        private byte[] keepAlive()
        {
            return vector3ToByte(Vector3.zero);
        }


        void getNetworkData()
        {
            //Recieve packets from other members in the lobby with us
            while (SteamNetworking.IsP2PPacketAvailable(out var msgSize))
            {
                byte[] packet = new byte[msgSize];
                CSteamID steamIDRemote;
                uint bytesRead = 0;
                if (SteamNetworking.ReadP2PPacket(packet, msgSize, out bytesRead, out steamIDRemote))
                {
                    int TYPE = packet[0];
                    if (PR.UI.isInStandaloneMenu) TYPE = 3; // If we're still in menu


                    var msg = SubArray(packet, 1, packet.Length - 1);

                    switch (TYPE)
                    {
                        case 1: //Packet type 1 == GOT IN-GAME CHAT MESSAGE
                            processIncoming(msg, 1, steamIDRemote);
                            break;
                        case 2:
                            processIncoming(msg, 2, steamIDRemote);
                            break;
                        case 3:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        void processIncoming(byte[] msg, int type, CSteamID steamIDRemote)
        {
            var vector3MSG = byteToVector3(msg);
            if (type == 1) playerTransforms[steamIDRemote].position = new Vector3(vector3MSG.x, vector3MSG.y - 2, vector3MSG.z);
            else if (type == 2) playerTransforms[steamIDRemote].eulerAngles = vector3MSG;
        }

        int getPlayerIndex(CSteamID input)
        {
            for (int i = 0; i < PR.SteamLobby.sArrSteamID_LobbyMembers.Length; i++)
                if (PR.SteamLobby.sArrSteamID_LobbyMembers[i] == input)
                    return i;
            return -1;
        }

        public void net_broadcast(int TYPE, byte[] b_message)
        {
            for (int i = 0; i < PR.SteamLobby.sArrSteamID_LobbyMembers.Length; i++)
            {
                if (PR.SteamLobby.sArrSteamID_LobbyMembers[i] == selfID) continue;
                byte[] sendBytes = new byte[b_message.Length + 1];
                sendBytes[0] = (byte) TYPE;
                b_message.CopyTo(sendBytes, 1);
                SteamNetworking.SendP2PPacket(PR.SteamLobby.sArrSteamID_LobbyMembers[i], sendBytes, (uint) sendBytes.Length, EP2PSend.k_EP2PSendReliable);
            }
        }

        void OnNewConnection(P2PSessionRequest_t result)
        {
            PR.SteamLobby.RefreshLobbyMembers();
            foreach (CSteamID id in PR.SteamLobby.sArrSteamID_LobbyMembers)
                if (id == result.m_steamIDRemote) //idremote is The Steam ID of the user that sent the initial packet to us.
                {
                    SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
                    return;
                }
        }


        private byte[] vector3ToByte(Vector3 vect)
        {
            byte[] buff = new byte[sizeof(float) * 3];
            Buffer.BlockCopy(BitConverter.GetBytes(vect.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vect.y), 0, buff, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vect.z), 0, buff, 2 * sizeof(float), sizeof(float));
            return buff;
        }

        private Vector3 byteToVector3(byte[] buff)
        {
            Vector3 vect = Vector3.zero;
            vect.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
            vect.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
            vect.z = BitConverter.ToSingle(buff, 2 * sizeof(float));
            return vect;
        }


        // IMPORTED
        public T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}