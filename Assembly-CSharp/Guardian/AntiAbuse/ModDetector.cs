﻿using Guardian.Networking;
using RC;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Guardian.AntiAbuse
{
    class ModDetector
    {
        public static void OnPlayerPropertyModified(object[] playerAndUpdatedProps)
        {
            PhotonPlayer player = playerAndUpdatedProps[0] as PhotonPlayer;
            ExitGames.Client.Photon.Hashtable properties = playerAndUpdatedProps[1] as ExitGames.Client.Photon.Hashtable;

            foreach (DictionaryEntry entry in properties)
            {
                switch (entry.Key as string)
                {
                    case "Ping":
                        // Ping
                        if (entry.Value is int ping)
                        {
                            player.Ping = ping;
                        }
                        break;
                    case "FoxMod":
                        // Fox Mod
                        player.IsFoxMod = true;
                        break;
                    case "guildName":
                        // Photon Mod
                        if (entry.Value is string guildName && (guildName.Equals("photonMod") || guildName.Equals("photonMod2")))
                        {
                            player.IsPhotonMod = true;
                        }
                        break;
                    default:
                        // Neko Mod
                        if (entry.Value is string value && (value.Equals("N_user") || value.Equals("N_owner")))
                        {
                            player.IsNekoMod = true;
                            player.IsNekoModUser = value.Equals("N_user");
                            player.IsNekoModOwner = value.Equals("N_owner");
                        }
                        break;
                }
            }
        }

        public static List<string> GetMods(PhotonPlayer player)
        {
            List<string> mods = new List<string>();
            ExitGames.Client.Photon.Hashtable properties = player.customProperties;

            // Neko
            if (player.IsNekoMod)
            {
                List<string> tags = new List<string>();
                if (player.IsNekoModUser)
                {
                    tags.Add("user");
                }
                if (player.IsNekoModOwner)
                {
                    tags.Add("owner");
                }
                mods.Add($"[EE00EE][Neko[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // Fox
            if (player.IsFoxMod)
            {
                mods.Add("[FF6600][Fox]");
            }

            // Cyrus Essentials
            if (player.IsCyrusMod)
            {
                mods.Add("[FFFF00][CE]");
            }

            // Anarchy
            if (player.IsAnarchyMod
                || (player.customProperties.ContainsKey("AnarchyFlags") && player.customProperties["AnarchyFlags"] is int)
                || (player.customProperties.ContainsKey("AnarchyAbuseFlags") && player.customProperties["AnarchyAbuseFlags"] is int))
            {
                mods.Add("[00BBCC][Anarchy]");
            }

            // KnK
            if (player.IsKNKMod)
            {
                mods.Add("[FF0000][KnK]");
            }

            // NRC
            if (player.IsNRCMod)
            {
                mods.Add("[FFFFFF][NRC]");
            }

            // TRAP
            if (player.IsTRAPMod)
            {
                mods.Add("[EE66FF][TRAP]");
            }

            // RC83
            if (player.IsRC83Mod)
            {
                mods.Add("[FFFFFF][RC83]");
            }

            // Guardian (mine!!)
            /* Notes:
             *  "GuardianMod" int = legacy Guardian identifier
             *  "Stats" int = legacy Guardian feature
             */
            if (properties.ContainsKey(GuardianPlayerProperty.GuardianMod))
            {
                List<string> tags = new List<string>();
                if (properties[GuardianPlayerProperty.GuardianMod] is string)
                {
                    tags.Add(GExtensions.AsString(properties[GuardianPlayerProperty.GuardianMod]));
                }
                else if (properties[GuardianPlayerProperty.GuardianMod] is int)
                {
                    tags.Add("legacy");
                }

                if (properties.ContainsKey(GuardianPlayerProperty.Stats) && properties[GuardianPlayerProperty.Stats] is int)
                {
                    List<char> modifications = ModifiedStats.FromInt(GExtensions.AsInt(properties[GuardianPlayerProperty.Stats]));
                    if (modifications.Count > 0)
                    {
                        tags.Add($"inf:{string.Join(",", modifications.Select(c => c.ToString()).ToArray())}");
                    }
                    else
                    {
                        tags.Add("inf:none");
                    }
                }

                mods.Add($"[FFBB00][Guardian[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // Alice-RC
            for (int i = 0; i < 5; i++)
            {
                if (player.customProperties.ContainsKey($"CO_SLOT_{i}") && player.customProperties[$"CO_SLOT_{i}"] is string)
                {
                    mods.Add("[FFCCFF][AliceRC]");
                    break;
                }
            }

            // ZMOD
            if (properties.ContainsKey("ZMOD") && properties["ZMOD"] is string
                && ((properties.ContainsKey("idleGas") && properties["idleGas"] is bool)
                || (properties.ContainsKey("idleEffect") && properties["idleEffect"] is string)
                || (properties.ContainsKey("infGas") && properties["infGas"] is bool)
                || (properties.ContainsKey("infBlades") && properties["infBlades"] is bool)
                || (properties.ContainsKey("infAHSS") && properties["infAHSS"] is bool)))
            {
                List<string> tags = new List<string>();
                if (properties.ContainsKey("infGas") && (bool)properties["infGas"])
                {
                    tags.Add("infGas");
                }
                if (properties.ContainsKey("infBlades") && (bool)properties["infBlades"])
                {
                    tags.Add("infBla");
                }
                if (properties.ContainsKey("infAHSS") && (bool)properties["infAHSS"])
                {
                    tags.Add("infAhss");
                }

                mods.Add($"[550055][ZMOD[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // Xeres
            if (properties.ContainsKey("Xeres") && GExtensions.AsString(properties["Xeres"]).Equals("yo mama perhaps")
                && ((properties.ContainsKey("infGas") && properties["infGas"] is bool)
                || (properties.ContainsKey("infBlades") && properties["infBlades"] is bool)
                || (properties.ContainsKey("infAHSS") && properties["infAHSS"] is bool)))
            {
                List<string> tags = new List<string>();
                if (properties.ContainsKey("infGas") && (bool)properties["infGas"])
                {
                    tags.Add("infGas");
                }
                if (properties.ContainsKey("infBlades") && (bool)properties["infBlades"])
                {
                    tags.Add("infBla");
                }
                if (properties.ContainsKey("infAHSS") && (bool)properties["infAHSS"])
                {
                    tags.Add("infAhss");
                }

                mods.Add($"[000000][Xeres[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // MorningStar
            if (properties.ContainsKey("Morningstar"))
            {
                mods.Add("[FFFFFF][MorningStar]");
            }

            // CatielRC
            if (properties.ContainsKey("CatielRC"))
            {
                mods.Add("[FFFFFF][CatielRC]");
            }

            // NudelBot
            if (properties.ContainsKey("NoodleDoodle"))
            {
                mods.Add("[FF6600][NudelBot]");
            }

            // AllStar
            if ((properties.ContainsKey("A.S Guard") && properties["A.S Guard"] is int)
                    || (properties.ContainsKey("Allstar Mod") && properties["Allstar Mod"] is int))
            {
                mods.Add("[FFFFFF][[FF0000]A[-]llStar]");
            }

            // DogS
            if (properties.ContainsKey("dogshitmod") && GExtensions.AsString(properties["dogshitmod"]).Equals("dogshitmod"))
            {
                mods.Add("[FFFFFF][DogS]");
            }

            // LNON
            if (properties.ContainsKey("LNON"))
            {
                mods.Add("[FFFFFF][LNON]");
            }

            // Ignis
            if (properties.ContainsKey("Ignis"))
            {
                mods.Add("[FFFFFF][Ignis]");
            }

            // Krab
            if (properties.ContainsKey("Krab"))
            {
                mods.Add("[FF0000][Krab]");
            }

            // PedoBear
            if (player.IsPBMod
                || properties.ContainsKey("PBModRC"))
            {
                mods.Add("[FF6600][Pedo[553300]Bear]");
            }

            // Disciple
            if (properties.ContainsKey("DiscipleMod"))
            {
                mods.Add("[777777][Disciple]");
            }

            // TLW
            if (properties.ContainsKey("TLW"))
            {
                mods.Add("[FFFFFF][TLW]");
            }

            // ARC
            if (properties.ContainsKey("ARC-CREADOR"))
            {
                mods.Add("[FFFFFF][ARC (Creator)]");
            }
            if (properties.ContainsKey("ARC"))
            {
                mods.Add("[FFFFFF][ARC]");
            }

            // SRC
            if (properties.ContainsKey("SRC"))
            {
                mods.Add("[FFFFFF][SRC]");
            }

            // Cyan Mod
            if (player.IsCyanMod
                || properties.ContainsKey("CyanMod")
                || properties.ContainsKey("CyanModNew"))
            {
                mods.Add("[00FFFF][Cyan]");
            }

            // Expedition
            if (player.IsExpeditionMod
                || properties.ContainsKey("ExpMod")
                || properties.ContainsKey("EMID")
                || properties.ContainsKey("Version")
                || properties.ContainsKey("Pref"))
            {
                List<string> tags = new List<string>();
                if (properties.ContainsKey("Version"))
                {
                    tags.Add(GExtensions.AsFloat(properties["Version"]).ToString());
                }
                if (properties.ContainsKey("Pref"))
                {
                    tags.Add(GExtensions.AsString(properties["Pref"]));
                }
                mods.Add($"[009900][Expedition[FFFFFF]({string.Join(string.Empty, tags.ToArray())})[-]]");
            }

            // Universe
            if (player.IsUniverseMod
                || properties.ContainsKey("UPublica")
                || properties.ContainsKey("UPublica2")
                || properties.ContainsKey("UGrup")
                || properties.ContainsKey("Hats")
                || properties.ContainsKey("UYoutube")
                || properties.ContainsKey("UVip")
                || properties.ContainsKey("SUniverse")
                || properties.ContainsKey("UAdmin")
                || properties.ContainsKey("coins")
                || (properties.ContainsKey(string.Empty) && properties[string.Empty] is string))
            {
                List<string> tags = new List<string>();
                if (properties.ContainsKey("UYoutube"))
                {
                    tags.Add("y[FF0000]t[-]");
                }
                if (properties.ContainsKey("UVip"))
                {
                    tags.Add("[FFCC00]vip[-]");
                }
                if (properties.ContainsKey("UAdmin"))
                {
                    tags.Add("[FF0000]admin[-]");
                }
                mods.Add($"[AA00AA][Universe[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // Teiko
            if (properties.ContainsKey("Teiko"))
            {
                mods.Add("[AED6F1][Teiko]");
            }

            // SLB
            if (properties.ContainsKey("Wings")
                || properties.ContainsKey("EarCat")
                || properties.ContainsKey("Horns"))
            {
                mods.Add("[FFFFFF][SLB]");
            }

            // Ranked RC
            if (player.IsRRCMod
                || properties.ContainsKey("bronze")
                || properties.ContainsKey("diamond")
                || (properties.ContainsKey(string.Empty) && properties[string.Empty] is int))
            {
                mods.Add("[FFFFFF][RRC]");
            }

            // DeadInside
            if (properties.ContainsKey("DeadInside"))
            {
                mods.Add("[000000][DeadInside]");
            }

            // DFSAO
            if (properties.ContainsKey("DFSAO"))
            {
                mods.Add("[FFFFFF][DFSAO]");
            }

            // AoE
            if (properties.ContainsKey("AOE") && GExtensions.AsString(properties["AOE"]).Equals("Made By Exile"))
            {
                mods.Add("[0000FF][AoE]");
            }

            // Fsociety
            // It's one of the two, I can't remember
            if (properties.ContainsKey("FSociety") || properties.ContainsKey("Fsociety"))
            {
                mods.Add("Fsociety");
            }

            if (player.IsPhotonMod)
            {
                mods.Add("Photon");
            }

            string name = GExtensions.AsString(player.customProperties[PhotonPlayerProperty.Name]);
            string guild = GExtensions.AsString(player.customProperties[PhotonPlayerProperty.Guild]);

            // Parrot
            if (guild.StartsWith("[00FF00]PARROT'S MOD"))
            {
                mods.Add("[00FF00][PARROT]");
            }

            // Unknown
            if (player.IsUnknownMod
                || properties.ContainsKey("FGT") // This might just be Bahaa's super-ban property, humorously enough I only see it on Beer.
                || properties.ContainsKey("me")
                || properties.ContainsKey("Taquila")
                || properties.ContainsKey("Pain")
                || properties.ContainsKey("uishot"))
            {
                mods.Add("[FFFFFF][???]");
            }

            // RC
            if (properties.ContainsKey(RCPlayerProperty.RCTeam)
                || properties.ContainsKey(RCPlayerProperty.RCBombR)
                || properties.ContainsKey(RCPlayerProperty.RCBombG)
                || properties.ContainsKey(RCPlayerProperty.RCBombB)
                || properties.ContainsKey(RCPlayerProperty.RCBombA)
                || properties.ContainsKey(RCPlayerProperty.RCBombRadius)
                || properties.ContainsKey(RCPlayerProperty.CustomBool)
                || properties.ContainsKey(RCPlayerProperty.CustomFloat)
                || properties.ContainsKey(RCPlayerProperty.CustomInt)
                || properties.ContainsKey(RCPlayerProperty.CustomString))
            {
                List<string> tags = new List<string>();
                if (player.IsNewRCMod)
                {
                    tags.Add("new");
                }

                mods.Add($"[9999FF][RC[FFFFFF]({string.Join(",", tags.ToArray())})[-]]");
            }

            // >48/>40 chars
            if (name.Length > 48 || guild.Length > 40)
            {
                string lengthFlags = string.Empty;

                if (name.Length > 48)
                {
                    lengthFlags += ">48";
                }
                if (guild.Length > 40)
                {
                    lengthFlags += name.Length > 48 ? "|>40" : ">40";
                }

                mods.Add($"[FFFFFF][{lengthFlags}]");
            }

            return mods;
        }
    }
}
