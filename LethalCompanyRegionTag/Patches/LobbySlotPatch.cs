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
    /// Reads the already-displayed server name from LobbyName.text
    /// instead of relying on lobby.Owner.Name (which is empty for non-friends).
    /// </summary>
    [HarmonyPatch(typeof(LobbySlot))]
    public static class LobbySlotPatch
    {
        // Track which lobby slots have been tagged (by instance hash)
        private static HashSet<int> _taggedSlots = new HashSet<int>();
        
        // Cache of analysis results by lobby ID
        private static Dictionary<ulong, Analysis.RegionResult> _resultCache = new Dictionary<ulong, Analysis.RegionResult>();
        
        // Track ongoing analysis to avoid duplicates
        private static HashSet<int> _pendingSlots = new HashSet<int>();

        /// <summary>
        /// Reset tagged slots (called when server list is refreshed).
        /// </summary>
        public static void ResetTaggedSlots()
        {
            _taggedSlots.Clear();
            _pendingSlots.Clear();
            _resultCache.Clear();
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

                int instanceHash = __instance.GetHashCode();

                // Skip if already tagged
                if (_taggedSlots.Contains(instanceHash))
                    return;

                // Skip if pending analysis
                if (_pendingSlots.Contains(instanceHash))
                    return;

                // Check if LobbyName text is available and has content
                var lobbyNameText = __instance.LobbyName;
                if (lobbyNameText == null)
                    return;

                string displayedName = lobbyNameText.text;
                if (string.IsNullOrEmpty(displayedName) || string.IsNullOrWhiteSpace(displayedName))
                    return;

                // Get lobby ID for caching
                var lobby = __instance.thisLobby;
                ulong lobbyIdValue = 0;
                try { lobbyIdValue = lobby.Id.Value; } catch { }

                // Check cache first
                if (lobbyIdValue != 0 && _resultCache.TryGetValue(lobbyIdValue, out var cachedResult))
                {
                    ApplyTag(__instance, cachedResult, displayedName);
                    _taggedSlots.Add(instanceHash);
                    return;
                }

                // Mark as pending
                _pendingSlots.Add(instanceHash);

                // Get owner Steam ID for additional queries
                ulong ownerSteamId = 0;
                try { ownerSteamId = lobby.Owner.Id.Value; } catch { }

                Plugin.LogSource.LogInfo($"[TAIGU] Analyzing slot: displayedName='{displayedName}', lobbyId={lobbyIdValue}, ownerSteamId={ownerSteamId}");

                // Run analysis using the DISPLAYED server name (not lobby.Owner.Name which is empty)
                var result = Analysis.RegionAnalyzer.AnalyzeRegion(displayedName, ownerSteamId).GetAwaiter().GetResult();

                Plugin.LogSource.LogInfo($"[TAIGU] Analysis result: primary={result.PrimaryRegion}, confidence={result.Confidence:F0}%, source={result.Source}");

                // Cache the result
                if (lobbyIdValue != 0)
                    _resultCache[lobbyIdValue] = result;

                // Apply the tag
                ApplyTag(__instance, result, displayedName);
                _taggedSlots.Add(instanceHash);
                _pendingSlots.Remove(instanceHash);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in LobbySlot.Update postfix: {ex.Message}\n{ex.StackTrace}");
                // Remove from pending on error so it can retry
                if (__instance != null)
                    _pendingSlots.Remove(__instance.GetHashCode());
            }
        }

        private static void ApplyTag(LobbySlot slot, Analysis.RegionResult result, string originalText)
        {
            try
            {
                if (slot.LobbyName == null)
                {
                    Plugin.LogSource.LogWarning("[TAIGU] LobbyName is null, cannot apply tag");
                    return;
                }

                // Build the tag string (ASCII only to avoid font issues)
                string tag = BuildTag(result);
                
                if (string.IsNullOrEmpty(tag))
                {
                    Plugin.LogSource.LogInfo("[TAIGU] No tag to apply (unknown region)");
                    return;
                }

                // Append tag to the original text
                string newText = $"{originalText}  {tag}";
                
                // Save original color and apply tag color
                var originalColor = slot.LobbyName.color;
                slot.LobbyName.text = newText;
                slot.LobbyName.color = GetTagColor(result.Confidence);

                Plugin.LogSource.LogInfo($"[TAIGU] Applied tag: '{tag}' to slot (confidence: {result.Confidence:F0}%)");

                // Restore original text color after a frame would be ideal,
                // but since we only do this once per slot, we keep the tag color
                // for the tag portion using rich text if supported
                if (slot.LobbyName.richText)
                {
                    // Use rich text to color only the tag portion
                    string colorHex = ColorToHex(GetTagColor(result.Confidence));
                    slot.LobbyName.text = $"{originalText}  <color=#{colorHex}>{tag}</color>";
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error applying tag: {ex.Message}");
            }
        }

        private static string BuildTag(Analysis.RegionResult result)
        {
            if (result == null || result.PrimaryRegion == "Unknown" || result.Confidence < 10f)
                return "";

            // Get country code or region abbreviation
            string code = result.CountryCode;
            if (string.IsNullOrEmpty(code))
            {
                code = GetRegionCode(result.PrimaryRegion);
            }

            // Build full probability distribution string
            // Format: [CN] 78% | JP 8% | KR 5% | Other 9%
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"[{code}] {result.Confidence:F0}%");

            if (result.Probabilities != null)
            {
                // Sort by probability descending, skip the primary region (already shown)
                var sorted = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>(result.Probabilities);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (var kvp in sorted)
                {
                    if (kvp.Value < 3f) continue; // Skip very small probabilities
                    if (kvp.Key == result.PrimaryRegion) continue; // Already shown as primary
                    
                    string regionCode = GetRegionCode(kvp.Key);
                    parts.Add($"{regionCode} {kvp.Value:F0}%");
                    
                    if (parts.Count >= 4) break; // Limit to 4 items max to avoid overflow
                }
            }

            return string.Join(" | ", parts);
        }

        private static string GetRegionCode(string region)
        {
            if (string.IsNullOrEmpty(region))
                return "??";

            // Map region names to short ASCII codes (2-5 chars max)
            // East Asia
            if (region.Contains("China (TW")) return "TW";
            if (region.Contains("China")) return "CN";
            if (region.Contains("Japan")) return "JP";
            if (region.Contains("Korea")) return "KR";
            if (region.Contains("Mongolia")) return "MN";

            // Southeast Asia
            if (region.Contains("Vietnam")) return "VN";
            if (region.Contains("Thailand")) return "TH";
            if (region.Contains("Indonesia")) return "ID";
            if (region.Contains("Philippines")) return "PH";
            if (region.Contains("Malaysia")) return "MY";
            if (region.Contains("Singapore")) return "SG";
            if (region.Contains("Cambodia")) return "KH";
            if (region.Contains("Laos")) return "LA";
            if (region.Contains("Myanmar")) return "MM";
            if (region.Contains("Southeast Asia")) return "SEA";

            // South Asia
            if (region.Contains("India")) return "IN";
            if (region.Contains("Pakistan")) return "PK";
            if (region.Contains("Bangladesh")) return "BD";
            if (region.Contains("Sri Lanka")) return "LK";
            if (region.Contains("Nepal")) return "NP";

            // Middle East / Central Asia
            if (region.Contains("Turkey")) return "TR";
            if (region.Contains("Saudi Arabia")) return "SA";
            if (region.Contains("UAE")) return "AE";
            if (region.Contains("Iran")) return "IR";
            if (region.Contains("Israel")) return "IL";
            if (region.Contains("Middle East")) return "MENA";
            if (region.Contains("Kazakhstan")) return "KZ";
            if (region.Contains("Georgia")) return "GE";
            if (region.Contains("Armenia")) return "AM";

            // Africa
            if (region.Contains("Egypt")) return "EG";
            if (region.Contains("Morocco")) return "MA";
            if (region.Contains("Algeria")) return "DZ";
            if (region.Contains("South Africa")) return "ZA";
            if (region.Contains("Ethiopia")) return "ET";
            if (region.Contains("North Africa")) return "NAF";

            // Russia / CIS
            if (region.Contains("Russia")) return "RU";
            if (region.Contains("Ukraine")) return "UA";
            if (region.Contains("Belarus")) return "BY";
            if (region.Contains("CIS")) return "CIS";

            // Eastern Europe
            if (region.Contains("Poland")) return "PL";
            if (region.Contains("Czech")) return "CZ";
            if (region.Contains("Slovakia")) return "SK";
            if (region.Contains("Hungary")) return "HU";
            if (region.Contains("Romania")) return "RO";
            if (region.Contains("Bulgaria")) return "BG";
            if (region.Contains("Croatia")) return "HR";
            if (region.Contains("Serbia")) return "RS";
            if (region.Contains("Baltics")) return "BAL";
            if (region.Contains("Eastern Europe")) return "EE";

            // Western Europe
            if (region.Contains("Germany")) return "DE";
            if (region.Contains("Austria")) return "AT";
            if (region.Contains("France")) return "FR";
            if (region.Contains("Italy")) return "IT";
            if (region.Contains("Iberia")) return "IB";
            if (region.Contains("Benelux")) return "BEN";
            if (region.Contains("UK") || region.Contains("Britain")) return "GB";
            if (region.Contains("Greece")) return "GR";
            if (region.Contains("Nordic")) return "NORD";
            if (region.Contains("Sweden")) return "SE";
            if (region.Contains("Norway")) return "NO";
            if (region.Contains("Denmark")) return "DK";
            if (region.Contains("Finland")) return "FI";
            if (region.Contains("Switzerland")) return "CH";

            // Americas
            if (region.Contains("North America") || region.Contains("USA")) return "US";
            if (region.Contains("Brazil")) return "BR";
            if (region.Contains("Mexico")) return "MX";
            if (region.Contains("Argentina")) return "AR";
            if (region.Contains("Latin America")) return "LAT";
            if (region.Contains("Americas")) return "AM";

            // Oceania
            if (region.Contains("Oceania")) return "OCE";
            if (region.Contains("Australia")) return "AU";

            // Western generic
            if (region.Contains("Western")) return "WEST";

            // Fallback: first 2-3 chars uppercase
            return region.Length >= 2 ? region.Substring(0, Math.Min(region.Length, 3)).ToUpper() : "??";
        }

        private static UnityEngine.Color GetTagColor(float confidence)
        {
            if (confidence >= 80f)
                return new UnityEngine.Color(0.2f, 1f, 0.2f); // Green - high confidence
            if (confidence >= 50f)
                return new UnityEngine.Color(1f, 1f, 0.2f); // Yellow - medium confidence
            if (confidence >= 20f)
                return new UnityEngine.Color(1f, 0.6f, 0.2f); // Orange - low confidence
            return new UnityEngine.Color(0.7f, 0.7f, 0.7f); // Gray - very low
        }

        private static string ColorToHex(UnityEngine.Color color)
        {
            byte r = (byte)(color.r * 255);
            byte g = (byte)(color.g * 255);
            byte b = (byte)(color.b * 255);
            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}
