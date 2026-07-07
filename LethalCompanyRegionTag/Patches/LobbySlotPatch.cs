using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches LobbySlot to add region tags to the display.
    /// </summary>
    [HarmonyPatch(typeof(LobbySlot))]
    public static class LobbySlotPatch
    {
        // Track which lobby IDs have been tagged
        private static HashSet<ulong> _taggedLobbyIds = new HashSet<ulong>();
        
        // Cache of analysis results
        private static Dictionary<ulong, Analysis.RegionResult> _resultCache = new Dictionary<ulong, Analysis.RegionResult>();
        
        // Track ongoing analysis tasks
        private static HashSet<ulong> _pendingAnalysis = new HashSet<ulong>();

        /// <summary>
        /// Reset tagged slots (called when server list is refreshed).
        /// </summary>
        public static void ResetTaggedSlots()
        {
            _taggedLobbyIds.Clear();
            _pendingAnalysis.Clear();
            Plugin.LogSource.LogInfo("[TAIGU] Reset tagged slots tracking");
        }

        /// <summary>
        /// Postfix for LobbySlot.Update - checks and tags each slot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(LobbySlot.Update))]
        static void PostfixUpdate(LobbySlot __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // Get the lobby data from the slot
                var lobby = __instance.thisLobby;
                ulong lobbyIdValue = lobby.Id.Value;

                // Skip if already tagged
                if (_taggedLobbyIds.Contains(lobbyIdValue))
                    return;

                // Skip if lobby ID is invalid
                if (lobbyIdValue == 0)
                    return;

                // Mark as pending to avoid duplicate analysis
                if (_pendingAnalysis.Contains(lobbyIdValue))
                    return;

                _pendingAnalysis.Add(lobbyIdValue);

                // Check cache first
                if (_resultCache.TryGetValue(lobbyIdValue, out var cachedResult))
                {
                    ApplyTag(__instance, cachedResult);
                    _taggedLobbyIds.Add(lobbyIdValue);
                    _pendingAnalysis.Remove(lobbyIdValue);
                    return;
                }

                // Start async analysis
                StartAnalysis(__instance, lobby);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in LobbySlot.Update postfix: {ex.Message}");
            }
        }

        private static void StartAnalysis(LobbySlot slot, Lobby lobby)
        {
            ulong lobbyIdValue = lobby.Id.Value;

            try
            {
                // Get owner info
                Friend owner = lobby.Owner;
                string ownerName = owner.Name ?? "Unknown";
                ulong ownerSteamId = owner.Id.Value;

                Plugin.LogSource.LogInfo($"[TAIGU] Analyzing lobby {lobbyIdValue}, owner: {ownerName}");

                // Run analysis synchronously for now (nickname analysis is fast)
                var result = Analysis.RegionAnalyzer.AnalyzeRegion(ownerName, ownerSteamId).GetAwaiter().GetResult();

                // Cache the result
                _resultCache[lobbyIdValue] = result;

                // Apply the tag
                ApplyTag(slot, result);
                _taggedLobbyIds.Add(lobbyIdValue);
                _pendingAnalysis.Remove(lobbyIdValue);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error analyzing lobby {lobbyIdValue}: {ex.Message}");
                _pendingAnalysis.Remove(lobbyIdValue);
            }
        }

        private static void ApplyTag(LobbySlot slot, Analysis.RegionResult result)
        {
            if (slot == null || slot.LobbyName == null)
                return;

            try
            {
                string originalName = slot.LobbyName.text ?? "";

                // Remove any existing tag we may have added
                originalName = RemoveExistingTag(originalName);

                if (result == null || result.PrimaryRegion == "Unknown" || result.Confidence < 20f)
                {
                    // Low confidence or unknown
                    if (Config.PluginConfig.ShowLowConfidenceTags.Value)
                    {
                        string tag = FormatTag("??", 0f);
                        slot.LobbyName.text = $"{tag} {originalName}";
                    }
                    return;
                }

                // Format the tag
                string regionCode = GetRegionCode(result.PrimaryRegion);
                float confidence = result.Confidence;

                // Only show tag if confidence is above threshold
                if (confidence >= Config.PluginConfig.MinConfidenceThreshold.Value)
                {
                    string tag = FormatTag(regionCode, confidence);
                    slot.LobbyName.text = $"{tag} {originalName}";
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error applying tag: {ex.Message}");
            }
        }

        private static string RemoveExistingTag(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove tags like [CN 78%] or [JP] or [??]
            int bracketStart = text.IndexOf('[');
            if (bracketStart == 0)
            {
                int bracketEnd = text.IndexOf(']');
                if (bracketEnd > bracketStart)
                {
                    return text.Substring(bracketEnd + 1).TrimStart();
                }
            }

            return text;
        }

        private static string FormatTag(string regionCode, float confidence)
        {
            if (Config.PluginConfig.ShowProbability.Value && confidence > 0f)
                return $"[{regionCode} {confidence:F0}%]";
            else
                return $"[{regionCode}]";
        }

        private static string GetRegionCode(string region)
        {
            if (string.IsNullOrEmpty(region))
                return "??";

            // Map region names to short codes
            if (region.Contains("China")) return "CN";
            if (region.Contains("Japan")) return "JP";
            if (region.Contains("Korea")) return "KR";
            if (region.Contains("Russia")) return "RU";
            if (region.Contains("Ukraine")) return "UA";
            if (region.Contains("North America")) return "NA";
            if (region.Contains("UK") || region.Contains("Ireland")) return "GB";
            if (region.Contains("Germany")) return "DE";
            if (region.Contains("France")) return "FR";
            if (region.Contains("Italy")) return "IT";
            if (region.Contains("Iberia") || region.Contains("Spain") || region.Contains("Portugal")) return "ES";
            if (region.Contains("Brazil")) return "BR";
            if (region.Contains("Oceania") || region.Contains("Australia")) return "AU";
            if (region.Contains("India")) return "IN";
            if (region.Contains("Thailand")) return "TH";
            if (region.Contains("Southeast Asia")) return "SEA";
            if (region.Contains("Turkey")) return "TR";
            if (region.Contains("Middle East")) return "ME";
            if (region.Contains("Latin America")) return "LATAM";
            if (region.Contains("Nordic")) return "NORD";
            if (region.Contains("Benelux")) return "BEN";
            if (region.Contains("Eastern Europe")) return "EE";
            if (region.Contains("CIS")) return "CIS";
            if (region.Contains("Greece")) return "GR";
            if (region.Contains("Israel")) return "IL";
            if (region.Contains("Poland")) return "PL";
            if (region.Contains("Western")) return "WEST";
            if (region.Contains("Americas")) return "AM";

            // Return first 2-4 chars of the region name
            return region.Length <= 4 ? region.ToUpper() : region.Substring(0, 3).ToUpper();
        }
    }
}
