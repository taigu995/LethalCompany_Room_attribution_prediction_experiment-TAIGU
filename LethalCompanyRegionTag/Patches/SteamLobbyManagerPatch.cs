using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches SteamLobbyManager to capture lobby list data.
    /// </summary>
    [HarmonyPatch(typeof(SteamLobbyManager))]
    public static class SteamLobbyManagerPatch
    {
        /// <summary>
        /// Current lobby list captured from the game.
        /// </summary>
        public static Lobby[] CurrentLobbyList { get; private set; }

        /// <summary>
        /// Postfix for loadLobbyListAndFilter - captures the full lobby list.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SteamLobbyManager.loadLobbyListAndFilter))]
        static void PostfixLoadLobbyList(Lobby[] lobbyList)
        {
            try
            {
                if (lobbyList == null || lobbyList.Length == 0)
                    return;

                CurrentLobbyList = lobbyList;
                Plugin.LogSource.LogInfo($"[TAIGU] Captured {lobbyList.Length} lobbies from server list");

                // Reset tagged slots so they get re-tagged
                LobbySlotPatch.ResetTaggedSlots();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in SteamLobbyManagerPatch: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix for LoadServerList - triggers when server list refresh is requested.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SteamLobbyManager.LoadServerList))]
        static void PostfixLoadServerList()
        {
            try
            {
                Plugin.LogSource.LogInfo("[TAIGU] Server list refresh requested");
                LobbySlotPatch.ResetTaggedSlots();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in LoadServerList postfix: {ex.Message}");
            }
        }
    }
}
