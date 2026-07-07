using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches LobbySlot to add region tags to the display.
    /// Two-phase strategy:
    ///   Phase 1 (instant): Nickname/language analysis from displayed server name
    ///   Phase 2 (async): Steam Community page query for exact country code
    /// </summary>
    [HarmonyPatch(typeof(LobbySlot))]
    public static class LobbySlotPatch
    {
        // Track which lobby slots have been tagged (by instance hash)
        private static HashSet<int> _taggedSlots = new HashSet<int>();
        
        // Track slots that have completed Phase 2 (Steam web query)
        private static HashSet<int> _webQueryDoneSlots = new HashSet<int>();
        
        // Cache of analysis results by lobby ID
        private static Dictionary<ulong, Analysis.RegionResult> _resultCache = new Dictionary<ulong, Analysis.RegionResult>();
        
        // Track ongoing async web queries to avoid duplicates
        private static HashSet<int> _pendingWebQueries = new HashSet<int>();

        /// <summary>
        /// Reset tagged slots (called when server list is refreshed).
        /// </summary>
        public static void ResetTaggedSlots()
        {
            _taggedSlots.Clear();
            _webQueryDoneSlots.Clear();
            _pendingWebQueries.Clear();
            _resultCache.Clear();
            Plugin.LogSource.LogInfo("[TAIGU] Reset tagged slots tracking");
        }

        /// <summary>
        /// Postfix for LobbySlot.Update - checks and tags each slot.
        /// Phase 1: Instant nickname analysis
        /// Phase 2: Async Steam Community query (upgrades the tag if successful)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(LobbySlot.Update))]
        static void PostfixUpdate(LobbySlot __instance)
        {
            try
            {
                // Process any pending UI updates from background web queries (main thread)
                ProcessPendingUpdates();

                if (__instance == null)
                    return;

                int instanceHash = __instance.GetHashCode();

                // Check if LobbyName text is available and has content
                var lobbyNameText = __instance.LobbyName;
                if (lobbyNameText == null)
                    return;

                string displayedName = lobbyNameText.text;
                if (string.IsNullOrEmpty(displayedName) || string.IsNullOrWhiteSpace(displayedName))
                    return;

                // Get lobby and owner info
                var lobby = __instance.thisLobby;
                ulong lobbyIdValue = 0;
                ulong ownerSteamId = 0;
                try { lobbyIdValue = lobby.Id.Value; } catch { }
                try { ownerSteamId = lobby.Owner.Id.Value; } catch { }

                // === Phase 1: Instant nickname analysis (runs once per slot) ===
                if (!_taggedSlots.Contains(instanceHash))
                {
                    // Check cache first
                    if (lobbyIdValue != 0 && _resultCache.TryGetValue(lobbyIdValue, out var cachedResult))
                    {
                        ApplyTag(__instance, cachedResult, displayedName);
                        _taggedSlots.Add(instanceHash);
                        // If cached result is from web query, skip Phase 2
                        if (cachedResult.Source == "Steam Community" || cachedResult.Source == "Steam XML" || cachedResult.Source == "Steam Web API")
                            _webQueryDoneSlots.Add(instanceHash);
                        return;
                    }

                    // Run quick nickname analysis (synchronous, instant)
                    var quickResult = Analysis.RegionAnalyzer.GetQuickAnalysis(displayedName);
                    
                    Plugin.LogSource.LogInfo($"[TAIGU] Phase 1 (nickname): displayedName='{displayedName}', primary={quickResult.PrimaryRegion}, confidence={quickResult.Confidence:F0}%");

                    // Apply the quick tag immediately
                    ApplyTag(__instance, quickResult, displayedName);
                    _taggedSlots.Add(instanceHash);

                    // Cache the quick result
                    if (lobbyIdValue != 0)
                        _resultCache[lobbyIdValue] = quickResult;

                    // === Phase 2: Start async Steam web query (if we have an owner Steam ID) ===
                    if (ownerSteamId != 0 && !_webQueryDoneSlots.Contains(instanceHash) && !_pendingWebQueries.Contains(instanceHash))
                    {
                        _pendingWebQueries.Add(instanceHash);
                        
                        Plugin.LogSource.LogInfo($"[TAIGU] Phase 2: Starting Steam web query for ownerSteamId={ownerSteamId}");
                        
                        // Start async web query - will update the tag when complete
                        StartWebQueryAsync(__instance, instanceHash, lobbyIdValue, ownerSteamId, displayedName);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in LobbySlot.Update postfix: {ex.Message}\n{ex.StackTrace}");
                if (__instance != null)
                    _pendingWebQueries.Remove(__instance.GetHashCode());
            }
        }

        /// <summary>
        /// Starts an async web query to get the owner's country code from Steam.
        /// Uses a background thread to avoid blocking the game.
        /// </summary>
        private static void StartWebQueryAsync(LobbySlot slot, int instanceHash, ulong lobbyId, ulong ownerSteamId, string displayedName)
        {
            // Use ThreadPool to run the async query without blocking the game thread
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Run the full async analysis (Steam Community + XML queries)
                    var webResult = Analysis.SteamWebQuery.QueryAllSources(ownerSteamId.ToString()).GetAwaiter().GetResult();
                    
                    if (webResult != null && !string.IsNullOrEmpty(webResult.CountryCode))
                    {
                        // Got a country code from Steam - build a high-confidence result
                        var enhancedResult = new Analysis.RegionResult
                        {
                            Source = webResult.Source,
                            CountryCode = webResult.CountryCode,
                            PrimaryRegion = Analysis.RegionAnalyzer.CountryCodeToRegion(webResult.CountryCode),
                            Confidence = webResult.Confidence,
                            Probabilities = BuildWebQueryProbabilityMap(webResult.CountryCode, webResult.Confidence)
                        };

                        Plugin.LogSource.LogInfo($"[TAIGU] Phase 2 complete: countryCode={webResult.CountryCode}, region={enhancedResult.PrimaryRegion}, confidence={enhancedResult.Confidence:F0}%, source={webResult.Source}");

                        // Update cache
                        if (lobbyId != 0)
                            _resultCache[lobbyId] = enhancedResult;

                        // Schedule UI update on main thread
                        // We use a coroutine-like approach with a flag
                        _pendingUpdateQueue.Enqueue(new PendingUpdate
                        {
                            InstanceHash = instanceHash,
                            Slot = slot,
                            Result = enhancedResult,
                            DisplayedName = displayedName
                        });
                        _hasPendingUpdate = true;
                    }
                    else
                    {
                        Plugin.LogSource.LogInfo($"[TAIGU] Phase 2: No country code found for ownerSteamId={ownerSteamId}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] Phase 2 error: {ex.Message}");
                }
                finally
                {
                    _pendingWebQueries.Remove(instanceHash);
                    _webQueryDoneSlots.Add(instanceHash);
                }
            });
        }

        // Thread-safe queue for pending UI updates
        private static Queue<PendingUpdate> _pendingUpdateQueue = new Queue<PendingUpdate>();
        private static volatile bool _hasPendingUpdate = false;

        private struct PendingUpdate
        {
            public int InstanceHash;
            public LobbySlot Slot;
            public Analysis.RegionResult Result;
            public string DisplayedName;
        }

        /// <summary>
        /// Called from the game's main thread to process pending UI updates.
        /// This should be called from a Harmony patch on a main-thread method.
        /// </summary>
        public static void ProcessPendingUpdates()
        {
            if (!_hasPendingUpdate) return;

            lock (_pendingUpdateQueue)
            {
                while (_pendingUpdateQueue.Count > 0)
                {
                    var update = _pendingUpdateQueue.Dequeue();
                    try
                    {
                        if (update.Slot != null && update.Slot.LobbyName != null)
                        {
                            ApplyTag(update.Slot, update.Result, update.DisplayedName);
                            Plugin.LogSource.LogInfo($"[TAIGU] Updated tag from web query: {update.Result.PrimaryRegion} ({update.Result.Confidence:F0}%)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogSource.LogWarning($"[TAIGU] Error processing pending update: {ex.Message}");
                    }
                }
                _hasPendingUpdate = false;
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

                // Build the tag string (uses Chinese names if CJK font available)
                string tag = BuildTag(result);
                
                if (string.IsNullOrEmpty(tag))
                {
                    Plugin.LogSource.LogInfo("[TAIGU] No tag to apply (unknown region)");
                    return;
                }

                // Apply CJK font support to the text component
                UI.FontManager.ApplyFontSupport(slot.LobbyName);

                // Append tag to the original text
                string newText = $"{originalText}  {tag}";
                
                // Save original color and apply tag color
                var originalColor = slot.LobbyName.color;
                slot.LobbyName.text = newText;
                slot.LobbyName.color = GetTagColor(result.Confidence);

                Plugin.LogSource.LogInfo($"[TAIGU] Applied tag: '{tag}' to slot (confidence: {result.Confidence:F0}%)");

                // Use rich text to color only the tag portion if supported
                if (slot.LobbyName.richText)
                {
                    string colorHex = ColorToHex(GetTagColor(result.Confidence));
                    slot.LobbyName.text = $"{originalText}  <color=#{colorHex}>{tag}</color>";
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error applying tag: {ex.Message}");
            }
        }

        /// <summary>
        /// Build probability map for web query results (high confidence, single country).
        /// </summary>
        private static Dictionary<string, float> BuildWebQueryProbabilityMap(string countryCode, float confidence)
        {
            var probs = new Dictionary<string, float>();
            string region = Analysis.RegionAnalyzer.CountryCodeToRegion(countryCode);
            probs[region] = confidence;
            
            float remaining = 100f - confidence;
            probs["Other"] = remaining;
            
            return probs;
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

            // Get display label (Chinese or English based on config)
            string displayLabel = GetDisplayLabel(code);

            // Add source indicator for web query results
            string sourceIndicator = "";
            if (result.Source == "Steam Community" || result.Source == "Steam XML" || result.Source == "Steam Web API")
            {
                sourceIndicator = "*"; // Asterisk indicates verified from Steam profile
            }

            // Build full probability distribution string
            // Format: [中国]* 95% | 其他 5%  (* = Steam verified)
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"[{displayLabel}]{sourceIndicator} {result.Confidence:F0}%");

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
                    string regionLabel = GetDisplayLabel(regionCode);
                    parts.Add($"{regionLabel} {kvp.Value:F0}%");
                    
                    if (parts.Count >= 4) break; // Limit to 4 items max to avoid overflow
                }
            }

            return string.Join(" | ", parts);
        }

        private static string GetRegionCode(string region)
        {
            if (string.IsNullOrEmpty(region))
                return "??";

            // Direct country code mapping
            string code = Analysis.RegionAnalyzer.RegionToCountryCode(region);
            if (!string.IsNullOrEmpty(code))
                return code;

            // Fallback abbreviations
            switch (region.ToLower())
            {
                case "china": return "CN";
                case "japan": return "JP";
                case "korea": return "KR";
                case "russia": return "RU";
                case "vietnam": return "VN";
                case "thailand": return "TH";
                case "western": return "WEST";
                case "eastern europe": return "EE";
                case "southeast asia": return "SEA";
                case "south asia": return "SA";
                case "central asia": return "CA";
                case "middle east": return "ME";
                case "south america": return "LATAM";
                case "central america": return "LATAM";
                case "north africa": return "NA";
                case "sub-saharan africa": return "SSA";
                case "oceania": return "OC";
                case "baltic": return "BLT";
                case "balkans": return "BALK";
                case "nordic": return "NORD";
                case "iberia": return "IB";
                default:
                    // Use first 2-4 chars of region name
                    if (region.Length <= 4) return region.ToUpper();
                    return region.Substring(0, 4).ToUpper();
            }
        }

        /// <summary>
        /// Region code to Chinese name mapping
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> RegionCodeToChinese =
            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                // East Asia
                { "CN", "中国" }, { "TW", "台湾" }, { "HK", "香港" },
                { "JP", "日本" }, { "KR", "韩国" },
                // Southeast Asia
                { "VN", "越南" }, { "TH", "泰国" }, { "ID", "印尼" },
                { "MY", "马来" }, { "PH", "菲律宾" }, { "SG", "新加坡" },
                { "KH", "柬埔寨" }, { "LA", "老挝" }, { "MM", "缅甸" },
                // South Asia
                { "IN", "印度" }, { "PK", "巴基斯坦" }, { "BD", "孟加拉" },
                // Central Asia
                { "KZ", "哈萨克" }, { "UZ", "乌兹别克" },
                // Middle East
                { "TR", "土耳其" }, { "SA", "沙特" }, { "AE", "阿联酋" },
                { "IR", "伊朗" }, { "IQ", "伊拉克" }, { "IL", "以色列" },
                // Eastern Europe
                { "RU", "俄罗斯" }, { "PL", "波兰" }, { "UA", "乌克兰" },
                { "CZ", "捷克" }, { "SK", "斯洛伐克" }, { "HU", "匈牙利" },
                { "RO", "罗马尼亚" }, { "BG", "保加利亚" },
                // Nordic
                { "SE", "瑞典" }, { "FI", "芬兰" }, { "DK", "丹麦" },
                // Western Europe
                { "GB", "英国" }, { "FR", "法国" }, { "DE", "德国" },
                { "IT", "意大利" }, { "ES", "西班牙" }, { "PT", "葡萄牙" },
                { "NL", "荷兰" }, { "BE", "比利时" }, { "AT", "奥地利" },
                // Americas
                { "US", "美国" }, { "CA", "加拿大" }, { "MX", "墨西哥" },
                { "BR", "巴西" }, { "AR", "阿根廷" }, { "CL", "智利" },
                { "CO", "哥伦比亚" }, { "PE", "秘鲁" },
                // Africa
                { "ZA", "南非" }, { "EG", "埃及" }, { "MA", "摩洛哥" },
                { "DZ", "阿尔及利亚" }, { "ET", "埃塞俄比亚" }, { "NG", "尼日利亚" },
                // Oceania
                { "AU", "澳洲" }, { "NZ", "新西兰" },
                // Mongolian
                { "MN", "蒙古" },
                // Aggregated regions
                { "WEST", "欧美" }, { "EE", "东欧" }, { "NORD", "北欧" },
                { "SA", "南亚" }, { "CA", "中亚" }, { "ME", "中东" },
                { "SEA", "东南亚" }, { "LATAM", "拉美" },
                { "NA", "北非" }, { "SSA", "南非" }, { "OC", "大洋洲" },
                { "BLT", "波罗的海" }, { "BALK", "巴尔干" }, { "IB", "伊比利亚" },
                { "Other", "其他" }, { "??", "未知" },
            };

        /// <summary>
        /// Get display label for a region code - Chinese or English based on config and font availability
        /// </summary>
        private static string GetDisplayLabel(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode))
                return "??";

            // Only use Chinese names if both config is enabled AND CJK font is available
            if (Config.PluginConfig.UseChineseDisplay.Value && UI.FontManager.CjkFontAvailable)
            {
                return UI.FontManager.GetDisplayLabel(regionCode);
            }

            return regionCode;
        }

        private static UnityEngine.Color GetTagColor(float confidence)
        {
            // Steam-verified results get a brighter color
            if (confidence >= 85f)
                return new UnityEngine.Color(0.2f, 1f, 0.2f); // Bright green (verified)
            if (confidence >= 60f)
                return new UnityEngine.Color(0.4f, 0.9f, 0.4f); // Green
            if (confidence >= 40f)
                return new UnityEngine.Color(1f, 0.9f, 0.3f); // Yellow
            return new UnityEngine.Color(0.7f, 0.7f, 0.7f); // Gray (low confidence)
        }

        private static string ColorToHex(UnityEngine.Color color)
        {
            int r = (int)(color.r * 255);
            int g = (int)(color.g * 255);
            int b = (int)(color.b * 255);
            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}
