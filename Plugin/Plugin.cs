using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    public partial class RollPostPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Roll Post Plug-In";              
        public const string Guid = "org.lordashes.plugins.rollpost";
        public const string Version = "1.3.3.0";

        // Configuration
        static string webURL { get; set; }
        static bool d20Only { get; set; }
        static bool localOnly { get; set; }

        void Awake()
        {
            UnityEngine.Debug.Log("Share Assets Plugin: "+this.GetType().AssemblyQualifiedName+" Active.");

            webURL = Config.Bind("Settings", "Web Application URL", "http://127.0.0.1/Dice.php").Value;
            d20Only = Config.Bind("Settings", "D20 Only", true).Value;
            localOnly = Config.Bind("Settings", "Local Player Only", true).Value;

            var harmony = new Harmony(Guid);
            harmony.PatchAll();

            Utility.PostOnMainPage(this.GetType());
        }

        public static void PostRoll(DiceManager.DiceRollResultData diceResult, string client)
        {
            Debug.Log("Roll Post Plugin: Posting Callback (Player="+ CampaignSessionManager.GetPlayerName(LocalPlayer.Id)+", Roller="+client+")");
            if (client != CampaignSessionManager.GetPlayerName(LocalPlayer.Id) && localOnly)
            {
                Debug.Log("Roll Post Plugin: Roll Made By Other Player - Not Posting Result");
            }
            else
                if (client == CampaignSessionManager.GetPlayerName(LocalPlayer.Id))
                {
                    Debug.Log("Roll Post Plugin: Roll Made By Local Player - Posting Die Roll");
                }
                else
                {
                    Debug.Log("Roll Post Plugin: Roll Made By Other Player - Posting Die Roll");
                }
                if (d20Only)
                {
                    Dictionary<string, string> kvp = new Dictionary<string, string>();
                    foreach (DiceManager.DiceGroupResultData group in diceResult.GroupResults)
                    {
                        Debug.Log("Roll Post Plugin: Group " + Convert.ToString(group.Name));
                        foreach (DiceManager.DiceResultData dice in group.Dice)
                        {
                            Debug.Log("Roll Post Plugin: Dice " + Convert.ToString(dice.Resource));
                            if (dice.Resource.Contains("20"))
                            {
                                kvp.Add("name", group.Name);
                                kvp.Add("formula", dice.Resource.Replace("numbered", "") + (dice.DiceOperator == DiceManager.DiceOperator.Add ? "+" : "-") + dice.Modifier);
                                kvp.Add("die", dice.Results[0].ToString());
                                kvp.Add("total", (dice.Results[0] + ((dice.DiceOperator == DiceManager.DiceOperator.Add ? 1 : -1) * dice.Modifier)).ToString());
                                break;
                            }
                        }
                        if (kvp.Count > 0) { break; }
                    }
                    if (kvp.Count > 0)
                    {
                        CreatureBoardAsset asset;
                        CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string content = "{\r\n"
                                           + "  \"player\": \"" + client + "\",\r\n"
                                           + "  \"name\": \"" + kvp["name"] + "\",\r\n"
                                           + "  \"mini\": \"" + GetName(asset) + "\",\r\n"
                                           + "  \"id\": \"" + ((asset != null) ? asset.CreatureId.ToString() : "") + "\",\r\n"
                                           + "  \"formula\": \"" + kvp["formula"] + "\",\r\n"
                                           + "  \"die\": \"" + kvp["die"] + "\",\r\n"
                                           + "  \"total\": \"" + kvp["total"] + "\"\r\n"
                                           + "}\r\n";
                            Debug.Log("Roll Post Plugin: Plugin Player Name = " + CampaignSessionManager.GetPlayerName(LocalPlayer.Id));
                            Debug.Log("Roll Post Plugin: Posting => " + content);
                            string HtmlResult = wc.UploadString(webURL, content);
                        }
                    }
                }
                else
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                        string HtmlResult = wc.UploadString(webURL, JsonConvert.SerializeObject(diceResult));
                    }
                }
            }
        }

        public static string GetName(CreatureBoardAsset asset)
        {
            if (asset == null) { return "(Not Selected)"; }
            string name = asset.Name;
            if (name.IndexOf("<size=0>") > -1) { name = name.Substring(0, name.IndexOf("<size=0>")); }
            return name;
        }
    }
}

