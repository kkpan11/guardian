﻿using Guardian.AntiAbuse;
using Guardian.AntiAbuse.Validators;
using Guardian.Features.Commands;
using Guardian.Features.Properties;
using Guardian.Features.Gamemodes;
using Guardian.Networking;
using Guardian.UI.Toasts;
using Guardian.Utilities;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Guardian
{
    class GuardianClient : MonoBehaviour
    {
        public static readonly string Build = "1.2.1";
        public static readonly string RootDir = Application.dataPath + "\\..";

        public static readonly CommandManager Commands = new CommandManager();
        public static readonly GamemodeManager Gamemodes = new GamemodeManager();
        public static readonly PropertyManager Properties = new PropertyManager();
        public static readonly FrameCounter FpsCounter = new FrameCounter();
        public static readonly ToastManager Toasts = new ToastManager();

        public static readonly Logger Logger = new Logger();
        public static UI.GuiController GuiController;
        public static readonly Regex BlacklistedTagsPattern = new Regex("<\\/?(size|material|quad)[^>]*>", RegexOptions.IgnoreCase);
        public static bool WasQuitRequested = false;
        public static string SystemLanguage => CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        private static bool IsFirstInit = true;
        private static bool HasJoinedRoom = false;

        private void Start()
        {
            // Load custom textures and audio clips
            {
                if (ResourceLoader.TryGetAsset("Custom/Textures/hud.png", out Texture2D hudTextures))
                {
                    GameObject backgroundGo = GameObject.Find("Background");
                    if (backgroundGo != null)
                    {
                        Material uiMat = backgroundGo.GetComponent<UISprite>().material;
                        uiMat.mainTextureScale = hudTextures.GetScaleVector(2048, 2048);
                        uiMat.mainTexture = hudTextures;
                    }
                }

                StartCoroutine(CoWaitAndSetParticleTexture());
            }

            GuiController = base.gameObject.AddComponent<UI.GuiController>();
            base.gameObject.AddComponent<MicEF>();

            if (!IsFirstInit) return;
            IsFirstInit = false;

            // Load network validation service
            NetworkValidator.Init();

            // Load skin validation service
            SkinValidator.Init();

            // Load name and guild (if possible)
            FengGameManagerMKII.NameField = PlayerPrefs.GetString("name", string.Empty);
            if (FengGameManagerMKII.NameField.StripNGUI().Length < 1)
            {
                FengGameManagerMKII.NameField = LoginFengKAI.Player.Name;
            }
            LoginFengKAI.Player.Guild = PlayerPrefs.GetString("guildname", string.Empty);

            // Load various features
            Commands.Load();
            Gamemodes.Load();
            Properties.Load();

            DiscordRPC.StartTime = GameHelper.CurrentTimeMillis();
            DiscordRPC.Initialize();

            // Check for an update
            StartCoroutine(CoCheckForUpdate());
        }

        private IEnumerator CoCheckForUpdate()
        {
            Logger.Info("Checking for update...");
            Logger.Info($"Installed: {Build}");

            using WWW www = new WWW("http://aottg.winnpixie.xyz/clients/guardian/version.txt?t=" + GameHelper.CurrentTimeMillis()); // Random long to try and avoid cache issues
            yield return www;

            if (www.error != null)
            {
                Logger.Error(www.error);

                Logger.Error($"\nIf errors persist, PLEASE contact me!");
                Logger.Info("Discord:");
                Logger.Info($"\t- {"https://discord.gg/JGzTdWm".AsColor("0099FF")}");

                try
                {
                    GameObject.Find("VERSION").GetComponent<UILabel>().text = "[FF0000]COULD NOT VERIFY BUILD.[-] If this persists, PLEASE contact me @ [0099FF]https://discord.gg/JGzTdWm[-]!";
                }
                catch { }
            }
            else
            {
                string latestBuild = "";
                foreach (string buildData in www.text.Split('\n'))
                {
                    string[] buildInfo = buildData.Split(new char[] { '=' }, 2);
                    if (!buildInfo[0].Equals("MOD")) continue;

                    latestBuild = buildInfo[1].Trim();
                }
                Logger.Info("Latest: " + latestBuild);

                if (!latestBuild.Equals(Build))
                {
                    Toasts.Add(new Toast("SYSTEM", "Your copy of Guardian is OUT OF DATE, please update!", 20));

                    Logger.Info($"Your copy of Guardian is {"OUT OF DATE".AsBold().AsItalic().AsColor("FF0000")}!");
                    Logger.Info("If you don't have the launcher, download it here:");
                    Logger.Info($"\t- {"https://cb.run/GuardianAoT".AsColor("0099FF")}");

                    try
                    {
                        GameObject.Find("VERSION").GetComponent<UILabel>().text = "[FF0000]OUT OF DATE![-] Please update from the launcher @ [0099FF]https://cb.run/GuardianAoT[-]!";
                    }
                    catch { }
                }
            }
        }

        private IEnumerator CoWaitAndSetParticleTexture()
        {
            // Load custom textures and audio clips
            ResourceLoader.TryGetAsset("Custom/Textures/dust.png", out Texture2D dustTexture);
            ResourceLoader.TryGetAsset("Custom/Textures/blood.png", out Texture2D bloodTexture);
            ResourceLoader.TryGetAsset("Custom/Textures/gun_smoke.png", out Texture2D gunSmokeTexture);

            for (; ; )
            {
                foreach (ParticleSystem ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
                {
                    if (dustTexture != null
                        && (ps.name.Contains("smoke")
                            || ps.name.StartsWith("boom")
                            || ps.name.StartsWith("bite")
                            || ps.name.StartsWith("Particle System 2")
                            || ps.name.StartsWith("Particle System 3")
                            || ps.name.StartsWith("Particle System 4")
                            || ps.name.Contains("colossal_steam")
                            || ps.name.Contains("FXtitan")
                            || ps.name.StartsWith("dust"))
                        && !ps.name.StartsWith("3dmg"))
                    {
                        ps.renderer.material.mainTexture = dustTexture;
                    }

                    if (bloodTexture != null && ps.name.Contains("blood"))
                    {
                        ps.renderer.material.mainTexture = bloodTexture;
                    }

                    if (gunSmokeTexture != null && ps.name.Contains("shotGun"))
                    {
                        ps.renderer.material.mainTexture = gunSmokeTexture;
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        public void ApplyCustomRenderSettings()
        {
            Properties.DrawDistance.OnValueChanged();
            Properties.Blur.OnValueChanged();
            // Custom MainLight Color is handled in IN_GAME_MAIN_CAMERA
            Properties.Fog.OnValueChanged();
            Properties.FogColor.OnValueChanged();
            Properties.FogDensity.OnValueChanged();
            Properties.SoftShadows.OnValueChanged();
        }

        private void Update()
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.CurrentMode.OnUpdate();
            }

            DiscordRPC.RunCallbacks();

            FpsCounter.UpdateCounter();
        }


        private void OnLevelWasLoaded(int level)
        {
            ApplyCustomRenderSettings();

            if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Singleplayer || PhotonNetwork.offlineMode)
            {
                string difficulty = IN_GAME_MAIN_CAMERA.Difficulty switch
                {
                    -1 => "Training",
                    0 => "Normal",
                    1 => "Hard",
                    2 => "Abnormal",
                    _ => "Unknown"
                };

                DiscordRPC.SetPresence(new Discord.Activity
                {
                    Details = $"Playing offline.",
                    State = $"{FengGameManagerMKII.Level.Name} / {difficulty}"
                });
            }

            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.CurrentMode.OnReset();
            }

            if (HasJoinedRoom) { return; }
            HasJoinedRoom = true;

            string joinMessage = Properties.JoinMessage.Value.NGUIToUnity();
            if (joinMessage.StripNGUI().Length < 1)
            {
                joinMessage = Properties.JoinMessage.Value;
            }

            if (joinMessage.Length < 1) return;
            Commands.Find("say").Execute(InRoomChat.Instance, joinMessage.Split(' '));
        }

        private void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.CurrentMode.OnPlayerJoin(player);
            }

            Logger.Info($"({player.Id}) " + player.Username.NGUIToUnity() + " connected.".AsColor("00FF00"));
        }

        private void OnPhotonPlayerDisconnected(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.CurrentMode.OnPlayerLeave(player);
            }

            Logger.Info($"({player.Id}) " + player.Username.NGUIToUnity() + " disconnected.".AsColor("FF0000"));
        }

        private void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
        {
            NetworkValidator.OnPlayerPropertyModification(playerAndUpdatedProps);

            ModDetector.OnPlayerPropertyModification(playerAndUpdatedProps);
        }

        private void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            NetworkValidator.OnRoomPropertyModification(propertiesThatChanged);

            PhotonPlayer sender = null;
            if (propertiesThatChanged.ContainsKey("sender") && propertiesThatChanged["sender"] is PhotonPlayer player)
            {
                sender = player;
            }

            if (sender != null && !sender.isMasterClient) return;

            if (propertiesThatChanged.ContainsKey("Map") && propertiesThatChanged["Map"] is string mapName)
            {
                LevelInfo levelInfo = LevelInfo.GetInfo(mapName);
                if (levelInfo != null)
                {
                    FengGameManagerMKII.Level = levelInfo;
                }
            }

            if (propertiesThatChanged.ContainsKey("Lighting") && propertiesThatChanged["Lighting"] is string lightLevel
                && GExtensions.TryParseEnum(lightLevel, out DayLight time))
            {
                Camera.main.GetComponent<IN_GAME_MAIN_CAMERA>().SetLighting(time);
            }
        }

        private void OnJoinedLobby()
        {
            // TODO: Begin working on Friend system with Photon Friend API
            PhotonNetwork.playerName = Properties.PhotonUserId.Value;
        }

        private void OnJoinedRoom()
        {
            HasJoinedRoom = false;

            // TODO: Potentially use custom event/group combo to sync game-settings whilst not triggering other mods
            int[] groups = new int[byte.MaxValue];
            for (int i = 0; i < byte.MaxValue; i++)
            {
                groups[i] = i + 1;
            }
            PhotonNetwork.SetReceivingEnabled(groups, null);
            PhotonNetwork.SetSendingEnabled(groups, null);

            PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                 { GuardianPlayerProperty.GuardianMod, Build }
            });

            StartCoroutine(CoUpdateMyPing());

            string[] roomInfo = PhotonNetwork.room.name.Split('`');
            if (roomInfo.Length < 7) return;

            DiscordRPC.SetPresence(new Discord.Activity
            {
                Details = $"Playing in {(roomInfo[5].Length < 1 ? string.Empty : "[PWD]")} {roomInfo[0].StripNGUI()}",
                State = $"({NetworkHelper.GetRegionCode().ToUpper()}) {roomInfo[1]} / {roomInfo[2].ToUpper()}"
            });
        }

        private IEnumerator CoUpdateMyPing()
        {
            while (PhotonNetwork.inRoom)
            {
                int currentPing = PhotonNetwork.player.Ping;
                int newPing = PhotonNetwork.GetPing();

                if (newPing != currentPing)
                {
                    PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                    {
                        { GuardianPlayerProperty.Ping, newPing }
                    });
                }

                yield return new WaitForSeconds(3f);
            }
        }

        private void OnLeftRoom()
        {
            Gamemodes.CurrentMode.CleanUp();

            PhotonNetwork.SetPlayerCustomProperties(null);

            // FIXME: Why don't these properly reset?
            RCSettings.BombCeiling = false;
            RCSettings.HideNames = false;

            DiscordRPC.SetPresence(new Discord.Activity
            {
                Details = "Idle..."
            });
        }

        private void OnConnectionFail(DisconnectCause cause)
        {
            Logger.Warn($"OnConnectionFail ({cause})");
        }

        private void OnPhotonRoomJoinFailed(object[] codeAndMsg)
        {
            Logger.Error($"OnPhotonRoomJoinFailed");

            foreach (object obj in codeAndMsg)
            {
                Logger.Error($" - {obj}");
            }
        }

        private void OnPhotonCreateRoomFailed(object[] codeAndMsg)
        {
            Logger.Error($"OnPhotonCreateRoomFailed");

            foreach (object obj in codeAndMsg)
            {
                Logger.Error($" - {obj}");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // UI.WindowManager.HandleWindowFocusEvent(hasFocus);

            if (!hasFocus
                || IN_GAME_MAIN_CAMERA.Gametype == GameType.Stop) return;

            // Minimap turns white
            if (Minimap.Instance != null)
            {
                Minimap.WaitAndTryRecaptureInstance(0.5f);
            }

            // TPS crosshair ending up where it shouldn't
            if (IN_GAME_MAIN_CAMERA.CameraMode == CameraType.TPS)
            {
                Screen.lockCursor = false;
                Screen.lockCursor = true;
            }
        }

        private void OnApplicationQuit()
        {
            WasQuitRequested = true;

            Properties.Save();

            DiscordRPC.Dispose();
        }
    }
}