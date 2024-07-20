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

[assembly: MelonInfo(typeof(DiscordStatus.Si_DiscordStatus), "Silica Discord Status", "1.0.1", "SlidyDev & databomb")]

namespace DiscordStatus
{
    public class Si_DiscordStatus : MelonPlugin
    {
        public const long AppId = 1129202364067364944;
        public Discord.Discord discordClient = null!;
        public ActivityManager activityManager = null!;
        static float timerRefreshActivity = -123.0f;
        static string currentMap = "Never-Never Land";

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

        public void UpdateActivity()
        {
            var activity = new Activity
            {
                Details = (NetworkGameServer.GetServerMap() == string.Empty) ? "Loading..." : $"Hosting {NetworkGameServer.GetServerMap()}"
            };

            activity.Assets.LargeImage = "silica512icon";
            
            activity.Name = NetworkGameServer.GetServerName();
            activity.Instance = true;
            activity.Assets.LargeText = activity.Name;

            var playerCount = NetworkGameServer.GetPlayersNum();
            var maxPlayers = NetworkGameServer.GetPlayersMax();
            activity.State = (maxPlayers == 0) ? "" : $"{playerCount} / {maxPlayers} Playing";

            // can the Join button work?
            /*if (maxPlayers != 0)
            {
                activity.Party.Id = NetworkServerLobby.GetLobbyID().m_SteamID.ToString();
                activity.Party.Size.CurrentSize = playerCount;
                activity.Party.Size.MaxSize = maxPlayers;
                activity.Secrets.Join = "=";
            }*/

            if (!string.Equals(currentMap, NetworkGameServer.GetServerMap()))
            {
                currentMap = NetworkGameServer.GetServerMap();
                gameStartedTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }

            if (GameStarted)
            {
                activity.Timestamps.Start = gameStartedTime;

            }

            activityManager.UpdateActivity(activity, ResultHandler);
        }

        public void ResultHandler(Result result)
        {

        }
    }
}
