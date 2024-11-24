/*
 * Copyright (c) 2022 Melon Enjoyers
 * Modified by databomb.
 *
 * MIT LICENSE
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 * 
 */

#if NET6_0
using Il2Cpp;
#endif

using MelonLoader;
using System;
using Discord;
using System.Threading;
using UnityEngine;
using HarmonyLib;

[assembly: MelonInfo(typeof(DiscordStatus.Si_DiscordStatus), "Silica Discord Status", "1.0.3", "SlidyDev & databomb")]

namespace DiscordStatus
{
    public class Si_DiscordStatus : MelonPlugin
    {
        public const long AppId = 1129202364067364944;
        public static Discord.Discord discordClient = null!;
        public static ActivityManager activityManager = null!;
        static float timerRefreshActivity = -123.0f;
        static string currentMap = "Never-Never Land";
        static string serverPassword = "";
        public static bool gameAdvertised = false;

        private bool gameClosing;
        public bool GameStarted { get; private set; }
        public long gameStartedTime;

        public override void OnPreInitialization()
        {
            DiscordLibraryLoader.LoadLibrary();
            InitializeDiscord();
            UpdateActivity();
            new Thread(DiscordLoopThread).Start();
        }

        public override void OnLateInitializeMelon()
        {
            timerRefreshActivity = 0.0f;
            GameStarted = true;
            gameStartedTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            UpdateActivity();
        }

        public override void OnApplicationQuit()
        {
            gameClosing = true;
        }

        public void DiscordLoopThread()
        {
            for (; ; )
            {
                if (gameClosing)
                    break;

                discordClient.RunCallbacks();
                Thread.Sleep(1000);
            }
        }

        public override void OnUpdate()
        {
            timerRefreshActivity += Time.deltaTime;
            if (timerRefreshActivity >= 5.0f)
            {
                timerRefreshActivity = 0.0f;

                UpdateActivity();
            }
        }

        public void InitializeDiscord()
        {
            discordClient = new Discord.Discord(AppId, (ulong)CreateFlags.NoRequireDiscord);
            discordClient.SetLogHook(LogLevel.Debug, DiscordLogHandler);

            activityManager = discordClient.GetActivityManager();

            activityManager.RegisterSteam(1494420U);
            activityManager.RegisterCommand("steam://run/1494420");
            activityManager.OnActivityJoin += OnActivityJoin;
            activityManager.OnActivityJoinRequest += OnActivityJoinRequest;
            activityManager.OnActivityInvite += (ActivityActionType type, ref User user,
                ref Activity activity) =>
            {
                MelonLogger.Msg($"Activity Invite: \n\tType:{type}\n\tUser:{user}\n\tActivity:{activity}");
            };
        }

        private void DiscordLogHandler(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    LoggerInstance.Msg(message);
                    break;

                case LogLevel.Warn:
                    LoggerInstance.Warning(message);
                    break;

                case LogLevel.Error:
                    LoggerInstance.Error(message);
                    break;
            }
        }

        [HarmonyPatch(typeof(NetworkGameServer), nameof(NetworkGameServer.SetAdvertiseServer))]
        private static class ApplyPatch_NetworkGameServer_SetAdvertiseServer
        {
            public static void Postfix(NetworkGameServer __instance, bool __0)
            {
                if (__0)
                {
                    gameAdvertised = true;
                }
            }
        }

        [HarmonyPatch(typeof(NetworkGameServer), nameof(NetworkGameServer.CreateServer))]
        private static class ApplyPatch_NetworkGameServer_CreateServer
        {
            public static void Postfix(bool __result, string __0, LevelInfo __1, GameModeInfo __2, int __3, bool __4, string __5)
            {
                serverPassword = __5;
            }
        }

        public void UpdateActivity()
        {
            if (!gameAdvertised)
            {
                var activityStart = new Activity
                {
                    Details = "Loading...",
                    Assets =
                    {
                        LargeImage = "silica512icon",
                        LargeText = NetworkGameServer.GetServerName(),
                    }
                };

                activityManager.UpdateActivity(activityStart, ResultHandler);
            }
            else
            {
                // update timer if the map changed
                if (!string.Equals(currentMap, NetworkGameServer.GetServerMap()))
                {
                    currentMap = NetworkGameServer.GetServerMap();
                    gameStartedTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                }

                var playerCount = NetworkGameServer.GetPlayersNum();
                var maxPlayers = NetworkGameServer.GetPlayersMax();

                var activity = new Activity
                {
                    State = (NetworkGameServer.GetServerMap() == string.Empty) ? "Switching Maps..." : $"In  {NetworkGameServer.GetServerMap()}",
                    Details = NetworkGameServer.GetServerName(),
                    Timestamps =
                    {
                        Start = gameStartedTime,
                    },
                    Assets =
                    {
                        LargeImage = "silica512icon",
                        LargeText = NetworkGameServer.GetServerName(),
                    },
                    Party =
                    {
                        Id = NetworkGameServer.GetServerID().m_ID.ToString(),
                        Size =
                        {
                            // 0 current players isn't supported by the Discord API
                            CurrentSize = playerCount + 1,
                            MaxSize = maxPlayers,
                        }
                    },
                    Secrets =
                    {
                        Join = NetworkGameServer.GetServerPasswordProtected() ? serverPassword : "no-secret",
                    },
                    Instance = true,
                };

                //activity.Name = NetworkGameServer.GetServerName();
                activityManager.UpdateActivity(activity, ResultHandler);
            }
        }

        public static void OnActivityJoin(string secret)
        {
            MelonLogger.Msg("Entered OnActivityJoin event with secret: " + secret);
        }

        public static void OnActivityJoinRequest(ref User user) => activityManager.SendRequestReply(
        user.Id, ActivityJoinRequestReply.Yes, (ActivityManager.SendRequestReplyHandler)(res =>
            {
                if (res != Result.Ok)
                    return;
                MelonLogger.Msg("Activity join request responded to with: Yes.");
            }));

        public void ResultHandler(Result result)
        {

        }
    }
}
