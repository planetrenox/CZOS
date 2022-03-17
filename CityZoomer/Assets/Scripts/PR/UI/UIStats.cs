using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PR
{
    public class UIStats : MonoBehaviour
    {
        // ommited from open source for time being


        private int detailsCount = 2;
        private static RawImage pfp1;
        private static RawImage pfp2;
        private static RawImage pfp3;
        private static RawImage pfp4;
        private static RawImage pfp5;
        private static TMP_Text name1;
        private static TMP_Text name2;
        private static TMP_Text name3;
        private static TMP_Text name4;
        private static TMP_Text name5;
        private static TMP_Text distance1;
        private static TMP_Text distance2;
        private static TMP_Text distance3;
        private static TMP_Text distance4;
        private static TMP_Text distance5;
        private static TMP_Text time1;
        private static TMP_Text time2;
        private static TMP_Text time3;
        private static TMP_Text time4;
        private static TMP_Text time5;

        private static RawImage pfp1_5k;
        private static RawImage pfp2_5k;
        private static RawImage pfp3_5k;
        private static RawImage pfp4_5k;
        private static RawImage pfp5_5k;
        private static TMP_Text name1_5k;
        private static TMP_Text name2_5k;
        private static TMP_Text name3_5k;
        private static TMP_Text name4_5k;
        private static TMP_Text name5_5k;
        private static TMP_Text time1_5k;
        private static TMP_Text time2_5k;
        private static TMP_Text time3_5k;
        private static TMP_Text time4_5k;
        private static TMP_Text time5_5k;

        private static TMP_Text textWeeklyTimeBanner;
        private static TMP_Text textWeeklyDistanceBanner;


        public struct LeaderboardData
        {
            public string username;
            public int rank;
            public int score;
            public int time;
            public int coins;
            public Steamworks.CSteamID steamuserid;
            public User steamPFP;
        }

        public struct LeaderboardDataForTime
        {
            public string username;
            public int rank;
            public int time;
            public Steamworks.CSteamID steamuserid;
            public User steamPFP;
        }


        public static int getCurrentWeek()
        {
            int startingWeek = -2;
            var daysSinceJan1st1970 = SteamUtils.GetServerRealTime() / 86400;
            var adjustedDays = daysSinceJan1st1970 - 4; // adjusting days so week starts on sunday
            return startingWeek + (int) ((adjustedDays / 7) - 2703); // starting at -2 on oct 25 2021, will change following sunday
        }


        private void Awake()
        {
            pfp1 = GameObject.Find("pfp1").GetComponent<RawImage>();
            pfp2 = GameObject.Find("pfp2").GetComponent<RawImage>();
            pfp3 = GameObject.Find("pfp3").GetComponent<RawImage>();
            pfp4 = GameObject.Find("pfp4").GetComponent<RawImage>();
            pfp5 = GameObject.Find("pfp5").GetComponent<RawImage>();
            name1 = GameObject.Find("name0").GetComponent<TMP_Text>();
            name2 = GameObject.Find("name0_1").GetComponent<TMP_Text>();
            name3 = GameObject.Find("name0_2").GetComponent<TMP_Text>();
            name4 = GameObject.Find("name0_3").GetComponent<TMP_Text>();
            name5 = GameObject.Find("name0_4").GetComponent<TMP_Text>();
            distance1 = GameObject.Find("Score1").GetComponent<TMP_Text>();
            distance2 = GameObject.Find("Score1_1").GetComponent<TMP_Text>();
            distance3 = GameObject.Find("Score1_2").GetComponent<TMP_Text>();
            distance4 = GameObject.Find("Score1_3").GetComponent<TMP_Text>();
            distance5 = GameObject.Find("Score1_4").GetComponent<TMP_Text>();
            time1 = GameObject.Find("Time1").GetComponent<TMP_Text>();
            time2 = GameObject.Find("Time1_1").GetComponent<TMP_Text>();
            time3 = GameObject.Find("Time1_2").GetComponent<TMP_Text>();
            time4 = GameObject.Find("Time1_3").GetComponent<TMP_Text>();
            time5 = GameObject.Find("Time1_4").GetComponent<TMP_Text>();
            pfp1_5k = GameObject.Find("1pfp5k").GetComponent<RawImage>();
            pfp2_5k = GameObject.Find("2pfp5k").GetComponent<RawImage>();
            pfp3_5k = GameObject.Find("3pfp5k").GetComponent<RawImage>();
            pfp4_5k = GameObject.Find("4pfp5k").GetComponent<RawImage>();
            pfp5_5k = GameObject.Find("5pfp5k").GetComponent<RawImage>();
            name1_5k = GameObject.Find("name1Time5K").GetComponent<TMP_Text>();
            name2_5k = GameObject.Find("name2Time5K").GetComponent<TMP_Text>();
            name3_5k = GameObject.Find("name3Time5K").GetComponent<TMP_Text>();
            name4_5k = GameObject.Find("name4Time5K").GetComponent<TMP_Text>();
            name5_5k = GameObject.Find("name5Time5K").GetComponent<TMP_Text>();
            time1_5k = GameObject.Find("1Time5K").GetComponent<TMP_Text>();
            time2_5k = GameObject.Find("2Time5K").GetComponent<TMP_Text>();
            time3_5k = GameObject.Find("3Time5K").GetComponent<TMP_Text>();
            time4_5k = GameObject.Find("4Time5K").GetComponent<TMP_Text>();
            time5_5k = GameObject.Find("5Time5K").GetComponent<TMP_Text>();
            textWeeklyTimeBanner = GameObject.Find("WeeklyLeaderboardTimeText").GetComponent<TMP_Text>();
            textWeeklyDistanceBanner = GameObject.Find("WeeklyLeaderboardText").GetComponent<TMP_Text>();
        }

        private float currBestDistance;
        private float currWeekBestDistance;


        private void Start()
        {
            // ommited from open source for time being
        }

        private void lateStart()
        {
            // ommited from open source for time being
        }


        public static void UpdateScoreAllTime(int score, int[] details)
        {
            // ommited from open source for time being
        }


        public static void UpdateScoreWeeklyDistance(int score, int[] details)
        {
            // ommited from open source for time being
        }

        public static void UpdateScore5K(float time)
        {
            // ommited from open source for time being
        }

        private static void UpdateCE()
        {
            // ommited from open source for time being
        }


        public static void GetLeaderBoardData(ELeaderboardDataRequest _type = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, int entries = 5)
        {
            // ommited from open source for time being
        }

        private static void onLeaderboardDownloadResult(LeaderboardScoresDownloaded_t pCallback, bool failure)
        {
            // ommited from open source for time being
        }

        private static void onLeaderboardDownloadResult5kTime(LeaderboardScoresDownloaded_t pCallback, bool failure)
        {
            // ommited from open source for time being
        }

        private static void fillLeaderboard(LeaderboardData lD, int index)
        {
            switch (index)
            {
                case 0:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp1.color;
                    tempColor.a = 1f;
                    pfp1.color = tempColor;
                    name1.text = lD.username;
                    distance1.text = lD.score.ToString();
                    time1.text = lD.time + "s";
                    pfp1.texture = user.SteamAvatarImage;
                    break;
                }
                case 1:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp2.color;
                    tempColor.a = 1f;
                    pfp2.color = tempColor;
                    name2.text = lD.username;
                    distance2.text = lD.score.ToString();
                    time2.text = lD.time + "s";
                    pfp2.texture = user.SteamAvatarImage;
                    break;
                }
                case 2:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp3.color;
                    tempColor.a = 1f;
                    pfp3.color = tempColor;
                    name3.text = lD.username;
                    distance3.text = lD.score.ToString();
                    time3.text = lD.time + "s";
                    pfp3.texture = user.SteamAvatarImage;
                    break;
                }
                case 3:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp4.color;
                    tempColor.a = 1f;
                    pfp4.color = tempColor;
                    name4.text = lD.username;
                    distance4.text = lD.score.ToString();
                    time4.text = lD.time + "s";
                    pfp4.texture = user.SteamAvatarImage;
                    break;
                }
                case 4:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp5.color;
                    tempColor.a = 1f;
                    pfp5.color = tempColor;
                    name5.text = lD.username;
                    distance5.text = lD.score.ToString();
                    time5.text = lD.time + "s";
                    pfp5.texture = user.SteamAvatarImage;
                    break;
                }
            }
        }

        private static void fillLeaderboard5kTime(LeaderboardDataForTime lD, int index)
        {
            switch (index)
            {
                case 0:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp1_5k.color;
                    tempColor.a = 1f;
                    pfp1_5k.color = tempColor;
                    name1_5k.text = lD.username;
                    time1_5k.text = lD.time + "s";
                    pfp1_5k.texture = user.SteamAvatarImage;
                    break;
                }
                case 1:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp2_5k.color;
                    tempColor.a = 1f;
                    pfp2_5k.color = tempColor;
                    name2_5k.text = lD.username;
                    time2_5k.text = lD.time + "s";
                    pfp2_5k.texture = user.SteamAvatarImage;
                    break;
                }
                case 2:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp3_5k.color;
                    tempColor.a = 1f;
                    pfp3_5k.color = tempColor;
                    name3_5k.text = lD.username;
                    time3_5k.text = lD.time + "s";
                    pfp3_5k.texture = user.SteamAvatarImage;
                    break;
                }
                case 3:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp4_5k.color;
                    tempColor.a = 1f;
                    pfp4_5k.color = tempColor;
                    name4_5k.text = lD.username;
                    time4_5k.text = lD.time + "s";
                    pfp4_5k.texture = user.SteamAvatarImage;
                    break;
                }
                case 4:
                {
                    User user = new User(lD.steamuserid);
                    var tempColor = pfp5_5k.color;
                    tempColor.a = 1f;
                    pfp5_5k.color = tempColor;
                    name5_5k.text = lD.username;
                    time5_5k.text = lD.time + "s";
                    pfp5_5k.texture = user.SteamAvatarImage;
                    break;
                }
            }
        }
    }
}