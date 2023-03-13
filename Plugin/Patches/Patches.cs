using BepInEx;
using GameChat.UI;
using HarmonyLib;

using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace LordAshes
{
    public partial class AutoRollPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(UIChatMessageManager), "AddDiceResultMessage")]
        public static class PatchAddDiceResultMessage
        {
            public static bool Prefix(DiceManager.DiceRollResultData diceResult, ClientGuid sender)
            {
                string playerName = "";
                PlayerGuid key;
                if (BoardSessionManager.ClientsPlayerGuids.TryGetValue(sender, out key))
                {
                    PlayerInfo playerInfo;
                    if (CampaignSessionManager.PlayersInfo.TryGetValue(key, out playerInfo))
                    {
                        playerName = playerInfo.Name;
                    }
                    else
                    {
                        playerName = CampaignSessionManager.GetPlayerName(LocalPlayer.Id);
                    }
                }
                RollPostPlugin.PostRoll(diceResult, playerName);
                return true;
            }
        }
    }
}
