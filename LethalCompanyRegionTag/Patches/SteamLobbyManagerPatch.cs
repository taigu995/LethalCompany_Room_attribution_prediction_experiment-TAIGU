using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using TMPro;
using LethalCompanyRegionTag.Analysis;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches for the SteamLobbyManager to intercept lobby list data.
    /// </summary>
    [HarmonyPatch]
    public static class SteamLobbyManagerPatch
    {
        // Store the current lobby data for UI rendering
        public static List<LobbyInfo> CurrentLobbies { get; private set; } = new List<LobbyInfo>();

        /// <summary>
        /// Postfix for LoadServerList to capture lobby data after it's loaded.
        /// </summary>
        [HarmonyPatch(typeof(SteamLobbyManager), "LoadServerList")]
        [HarmonyPostfix]
        public static void Postfix_LoadServerList(SteamLobbyManager __instance)
        {
            try
            {
                ExtractLobbyData(__instance);

                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogInfo($"[TAIGU] LoadServerList called, found {CurrentLobbies.Count} lobbies");
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[TAIGU] Error in LoadServerList patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix for loadLobbyListAndFilter to capture filtered lobby data.
        /// </summary>
        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyPostfix]
        public static void Postfix_LoadLobbyListAndFilter(SteamLobbyManager __instance)
        {
            try
            {
                ExtractLobbyData(__instance);

                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogInfo($"[TAIGU] loadLobbyListAndFilter called, found {CurrentLobbies.Count} lobbies");
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[TAIGU] Error in loadLobbyListAndFilter patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract lobby data from SteamLobbyManager using reflection.
        /// </summary>
        private static void ExtractLobbyData(SteamLobbyManager instance)
        {
            CurrentLobbies.Clear();

            // Try to access the lobby list via reflection
            // The game stores lobby data in fields like lobbyList, currentLobbyList, etc.
            var type = typeof(SteamLobbyManager);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Try to get the server list or lobby list
            // Facepunch.Steamworks uses Steamworks.ServerList for game server queries
            var lobbyListField = type.GetField("lobbyList", flags)
                ?? type.GetField("currentLobbyList", flags)
                ?? type.GetField("_lobbyList", flags);

            if (lobbyListField != null)
            {
                var lobbyList = lobbyListField.GetValue(instance);
                if (lobbyList is IList list)
                {
                    foreach (var item in list)
                    {
                        if (item == null) continue;

                        var itemInfo = ExtractLobbyInfo(item);
                        if (itemInfo != null)
                            CurrentLobbies.Add(itemInfo);
                    }
                }
            }

            // If no lobbies found via field, try to get them from the server list UI container
            if (CurrentLobbies.Count == 0)
            {
                ExtractFromUIContainer(instance);
            }
        }

        /// <summary>
        /// Extract info from a single lobby/server item using reflection.
        /// </summary>
        private static LobbyInfo ExtractLobbyInfo(object item)
        {
            try
            {
                var itemType = item.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var info = new LobbyInfo();

                // Try to get Steam ID (host/owner)
                // Facepunch.Steamworks Server has: SteamId, Name, Players, etc.
                var steamIdProp = itemType.GetProperty("SteamId", flags)
                    ?? itemType.GetProperty("steamId", flags);
                if (steamIdProp != null)
                {
                    var steamIdObj = steamIdProp.GetValue(item);
                    // SteamId in Facepunch is a struct with a Value property (ulong)
                    var valueProp = steamIdObj.GetType().GetProperty("Value", flags);
                    if (valueProp != null)
                        info.SteamId64 = (ulong)valueProp.GetValue(steamIdObj);
                    else if (steamIdObj is ulong ul)
                        info.SteamId64 = ul;
                    else if (steamIdObj is long l)
                        info.SteamId64 = (ulong)l;
                }

                // Try to get lobby name / server name
                var nameProp = itemType.GetProperty("Name", flags)
                    ?? itemType.GetProperty("name", flags)
                    ?? itemType.GetProperty("LobbyName", flags);
                if (nameProp != null)
                    info.Name = nameProp.GetValue(item)?.ToString() ?? "";

                // Try to get player count
                var playersProp = itemType.GetProperty("Players", flags)
                    ?? itemType.GetProperty("players", flags)
                    ?? itemType.GetProperty("MemberCount", flags);
                if (playersProp != null)
                {
                    var val = playersProp.GetValue(item);
                    if (val is int i) info.PlayerCount = i;
                    else if (val is byte b) info.PlayerCount = b;
                }

                // Try to get max players
                var maxProp = itemType.GetProperty("MaxPlayers", flags)
                    ?? itemType.GetProperty("maxPlayers", flags)
                    ?? itemType.GetProperty("MaxMembers", flags);
                if (maxProp != null)
                {
                    var val = maxProp.GetValue(item);
                    if (val is int i) info.MaxPlayers = i;
                    else if (val is byte b) info.MaxPlayers = b;
                }

                // Try to get ping
                var pingProp = itemType.GetProperty("Ping", flags)
                    ?? itemType.GetProperty("ping", flags);
                if (pingProp != null)
                {
                    var val = pingProp.GetValue(item);
                    if (val is int i) info.Ping = i;
                }

                // Only add if we have a valid Steam ID
                if (info.SteamId64 > 0)
                    return info;
            }
            catch (Exception ex)
            {
                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogWarning($"[TAIGU] Failed to extract lobby info: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Fallback: Extract lobby info from the UI container's LobbySlot components.
        /// </summary>
        private static void ExtractFromUIContainer(SteamLobbyManager instance)
        {
            try
            {
                var type = typeof(SteamLobbyManager);
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var containerField = type.GetField("serverListUIContainer", flags)
                    ?? type.GetField("lobbyListContainer", flags);

                if (containerField?.GetValue(instance) is Transform container)
                {
                    // Find all LobbySlot components in the container
                    var lobbySlots = container.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var slot in lobbySlots)
                    {
                        if (slot == null) continue;
                        var slotType = slot.GetType();

                        // Check if this is a LobbySlot
                        if (slotType.Name == "LobbySlot" || slotType.Name.Contains("LobbySlot"))
                        {
                            var info = ExtractLobbySlotInfo(slot);
                            if (info != null)
                                CurrentLobbies.Add(info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogWarning($"[TAIGU] ExtractFromUIContainer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract info from a LobbySlot MonoBehaviour.
        /// </summary>
        private static LobbyInfo ExtractLobbySlotInfo(MonoBehaviour slot)
        {
            try
            {
                var slotType = slot.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var info = new LobbyInfo();

                // Try to get Steam ID from the slot
                var steamIdField = slotType.GetField("steamId", flags)
                    ?? slotType.GetField("SteamId", flags)
                    ?? slotType.GetField("lobbyId", flags)
                    ?? slotType.GetField("hostSteamId", flags)
                    ?? slotType.GetField("ownerSteamId", flags);

                if (steamIdField != null)
                {
                    var val = steamIdField.GetValue(slot);
                    if (val is ulong ul) info.SteamId64 = ul;
                    else if (val is long l) info.SteamId64 = (ulong)l;
                    else if (val is Steamworks.SteamId sid) info.SteamId64 = sid.Value;
                    else
                    {
                        // Try to get Value property from SteamId struct
                        var valueProp = val.GetType().GetProperty("Value", flags);
                        if (valueProp != null)
                            info.SteamId64 = (ulong)valueProp.GetValue(val);
                    }
                }

                // Try to get name
                var nameField = slotType.GetField("lobbyName", flags)
                    ?? slotType.GetField("serverName", flags)
                    ?? slotType.GetField("name", flags);
                if (nameField != null)
                    info.Name = nameField.GetValue(slot)?.ToString() ?? "";

                if (info.SteamId64 > 0)
                    return info;
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Simplified lobby info extracted from the game.
    /// </summary>
    public class LobbyInfo
    {
        public ulong SteamId64 { get; set; }
        public string Name { get; set; } = "";
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public int Ping { get; set; }
        public RegionResult RegionResult { get; set; }
    }
}
