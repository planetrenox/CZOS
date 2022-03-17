using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Steamworks;

    public class User
    {
        public CSteamID SteamID;
        public int AvatarID;

        public Texture2D SteamAvatarImage { get; private set; }

        public string SteamUsername { get; private set; }

        Callback<PersonaStateChange_t> callPersona;
        Callback<AvatarImageLoaded_t> callAvatar;
        Callback<PersonaStateChange_t> personaState;

        public User(CSteamID id)
        {
            AvatarID = -1;
            SteamID = id;
            SteamUsername = SteamFriends.GetFriendPersonaName(id);
            if (SteamUsername == "" || SteamUsername == "[unknown]") LoadName();
            else DownloadAvatar();
        }

        void LoadName()
        {
            personaState = Callback<PersonaStateChange_t>.Create((cb) =>
            {
                if (SteamID == (CSteamID)cb.m_ulSteamID)
                {
                    SteamUsername = SteamFriends.GetFriendPersonaName(SteamID);
                    if (SteamUsername == "" || SteamUsername == "[unknown]") LoadName();
                    else DownloadAvatar();
                }
            });
        }

        void DownloadAvatar()
        {
            Texture2D tex = GetUserAvatar(SteamID);
            if (tex != null) SteamAvatarImage = tex;
        }

        Texture2D GetUserAvatar(CSteamID id)
        {
            int handler = SteamFriends.GetLargeFriendAvatar(id);
            switch (handler)
            {
                case -1:
                    callAvatar = Callback<AvatarImageLoaded_t>.Create((cb) =>
                    {
                        if (id == cb.m_steamID) AvatarLoaded(cb);
                    });
                    return SteamAvatarImage;
                case 0:
                    if (SteamFriends.RequestUserInformation(id, false))
                    {
                        callPersona = Callback<PersonaStateChange_t>.Create((cb) =>
                        {
                            if (id == (CSteamID)cb.m_ulSteamID) PersonaStateChangeRequest(cb);
                        });
                        return SteamAvatarImage;
                    }
                    else return GetTex(handler);
                default: return GetTex(handler);
            }
        }

        Texture2D GetTex(int handler)
        {
            uint width, height;

            if (SteamUtils.GetImageSize(handler, out width, out height))
            {
                byte[] data = new byte[width * height * 4];
                if (SteamUtils.GetImageRGBA(handler, data, data.Length))
                {
                    Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(data);
                    tex.Apply();
                    return tex;
                }
            }

            return null;
        }

        void PersonaStateChangeRequest(PersonaStateChange_t cb)
        {
            Texture2D tex = GetUserAvatar((CSteamID)cb.m_ulSteamID);
            if (tex != null) SteamAvatarImage = tex;
        }

        void AvatarLoaded(AvatarImageLoaded_t cb)
        {
            Texture2D tex = GetUserAvatar(cb.m_steamID);
            if (tex != null) SteamAvatarImage = tex;
        }
    }
}