using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches SteamLobbyManager to capture lobby list data.
    /// This patch was confirmed working in game logs.
    /// </summary>
    [HarmonyPatch(typeof(SteamLobbyManager))]
    public static class SteamLobbyManagerPatch
    {
        /// <summary>
        /// Postfix for loadLobbyListAndFilter - captures the full lobby list
        /// after Steam returns search results.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SteamLobbyManager.loadLobbyListAndFilter))]
        static void PostfixLoadLobbyList(Lobby[] lobbyList)
        {
            try
            {
                if (lobbyList == null || lobbyList.Length == 0)
                    return;

                Plugin.LogSource.LogInfo($"[TAIGU] Captured {lobbyList.Length} lobbies from server list");

                // Notify RegionTagManager about new lobby list
                if (UI.RegionTagManager.Instance != null)
                {
                    UI.RegionTagManager.Instance.OnLobbyListReceived(lobbyList);
                }
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
                Plugin.LogSource.LogInfo("[TAIGU] Server list refresh requested, clearing cache for stale entries");
                // Clear the tagged slots tracking so new slots get tagged
                if (UI.RegionTagManager.Instance != null)
                {
                    UI.RegionTagManager.Instance.ClearTaggedSlots();
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in LoadServerList postfix: {ex.Message}");
            }
        }
    }
}
