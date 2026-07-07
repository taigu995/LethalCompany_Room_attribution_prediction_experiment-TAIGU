using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using TMPro;
using PluginCfg = LethalCompanyRegionTag.Config.PluginConfig;

namespace LethalCompanyRegionTag.UI
{
    /// <summary>
    /// MonoBehaviour that monitors LobbySlot instances and adds region tags.
    /// Runs in the game scene, checking for new LobbySlots each frame.
    /// </summary>
    public class RegionTagManager : MonoBehaviour
    {
        public static RegionTagManager Instance { get; private set; }

        // Track which LobbySlots have been tagged (by lobby ID)
        private readonly HashSet<ulong> _taggedLobbyIds = new HashSet<ulong>();
        // Cache of analysis results by lobby ID
        private readonly Dictionary<ulong, Analysis.RegionResult> _resultCache = new Dictionary<ulong, Analysis.RegionResult>();
        // Track ongoing analysis tasks
        private readonly HashSet<ulong> _pendingAnalysis = new HashSet<ulong>();
        // Current lobby list reference
        private Lobby[] _currentLobbyList;

        private float _lastCheckTime;
        private const float CHECK_INTERVAL = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Plugin.LogSource.LogInfo("[TAIGU] RegionTagManager initialized");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Called by SteamLobbyManagerPatch when a new lobby list is received.
        /// </summary>
        public void OnLobbyListReceived(Lobby[] lobbyList)
        {
            _currentLobbyList = lobbyList;
            Plugin.LogSource.LogInfo($"[TAIGU] Lobby list updated with {lobbyList?.Length ?? 0} entries");
        }

        /// <summary>
        /// Clear tagged slots tracking (called when server list is refreshed).
        /// </summary>
        public void ClearTaggedSlots()
        {
            _taggedLobbyIds.Clear();
            _pendingAnalysis.Clear();
            Plugin.LogSource.LogInfo("[TAIGU] Cleared tagged slots tracking");
        }

        private void Update()
        {
            // Throttle checks to avoid performance impact
            if (Time.time - _lastCheckTime < CHECK_INTERVAL)
                return;
            _lastCheckTime = Time.time;

            try
            {
                // Find all active LobbySlot instances in the scene
                var lobbySlots = FindObjectsOfType<LobbySlot>();
                if (lobbySlots == null || lobbySlots.Length == 0)
                    return;

                foreach (var slot in lobbySlots)
                {
                    if (slot == null)
                        continue;

                    ulong lobbyIdValue = slot.lobbyId.Value;

                    // Skip if already tagged
                    if (_taggedLobbyIds.Contains(lobbyIdValue))
                        continue;

                    // Check if this slot has valid lobby data
                    var lobby = slot.thisLobby;
                    if (lobby.Id.Value == 0)
                        continue;

                    // Start analysis for this lobby
                    if (!_pendingAnalysis.Contains(lobbyIdValue))
                    {
                        _pendingAnalysis.Add(lobbyIdValue);
                        StartCoroutine(AnalyzeAndTagLobby(slot, lobby));
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error in RegionTagManager.Update: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes a lobby's owner and tags the UI element.
        /// </summary>
        private IEnumerator AnalyzeAndTagLobby(LobbySlot slot, Lobby lobby)
        {
            ulong lobbyIdValue = lobby.Id.Value;

            // Check cache first
            if (_resultCache.TryGetValue(lobbyIdValue, out var cachedResult))
            {
                ApplyTagToSlot(slot, cachedResult);
                _taggedLobbyIds.Add(lobbyIdValue);
                _pendingAnalysis.Remove(lobbyIdValue);
                yield break;
            }

            // Get owner info from the Lobby struct
            Friend owner = lobby.Owner;
            string ownerName = owner.Name ?? "Unknown";
            ulong ownerSteamId = owner.Id.Value;

            Plugin.LogSource.LogInfo($"[TAIGU] Analyzing lobby {lobbyIdValue}, owner: {ownerName} (SteamID: {ownerSteamId})");

            // Run analysis asynchronously
            Analysis.RegionResult result = null;

            // Start the async analysis
            var analysisTask = Analysis.RegionAnalyzer.AnalyzeRegion(ownerName, ownerSteamId);

            // Wait for analysis to complete (with timeout)
            float timeout = 5f;
            float elapsed = 0f;
            while (!analysisTask.IsCompleted && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (analysisTask.IsCompleted)
            {
                result = analysisTask.Result;
            }
            else
            {
                // Timeout - use nickname analysis only
                result = Analysis.RegionAnalyzer.GetQuickAnalysis(ownerName);
                Plugin.LogSource.LogWarning($"[TAIGU] Analysis timeout for lobby {lobbyIdValue}, using quick analysis");
            }

            // Cache the result
            _resultCache[lobbyIdValue] = result;

            // Apply the tag to the UI (outside try-catch to avoid yield restriction)
            ApplyTagToSlot(slot, result);
            _taggedLobbyIds.Add(lobbyIdValue);
            _pendingAnalysis.Remove(lobbyIdValue);
        }

        /// <summary>
        /// Applies a region tag to a LobbySlot's display name.
        /// Uses ASCII-only text to avoid font compatibility issues.
        /// </summary>
        private void ApplyTagToSlot(LobbySlot slot, Analysis.RegionResult result)
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
                    // Low confidence or unknown - show minimal tag
                    if (PluginCfg.ShowLowConfidenceTags.Value)
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
                if (confidence >= PluginCfg.MinConfidenceThreshold.Value)
                {
                    string tag = FormatTag(regionCode, confidence);
                    slot.LobbyName.text = $"{tag} {originalName}";

                    // Color the tag based on confidence
                    UnityEngine.Color tagColor = GetConfidenceColor(confidence);
                    slot.LobbyName.color = tagColor;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Error applying tag to slot: {ex.Message}");
            }
        }

        /// <summary>
        /// Formats a region tag string (ASCII only for font compatibility).
        /// </summary>
        private string FormatTag(string regionCode, float confidence)
        {
            if (PluginCfg.ShowProbability.Value && confidence > 0f)
                return $"[{regionCode} {confidence:F0}%]";
            else
                return $"[{regionCode}]";
        }

        /// <summary>
        /// Gets a short region code for display.
        /// </summary>
        private string GetRegionCode(string region)
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

        /// <summary>
        /// Gets a color based on confidence level.
        /// </summary>
        private UnityEngine.Color GetConfidenceColor(float confidence)
        {
            if (confidence >= 80f)
                return new UnityEngine.Color(0.2f, 1f, 0.2f); // Bright green - high confidence
            if (confidence >= 60f)
                return new UnityEngine.Color(0.8f, 1f, 0.2f); // Yellow-green - medium-high
            if (confidence >= 40f)
                return new UnityEngine.Color(1f, 0.8f, 0.2f); // Yellow - medium
            if (confidence >= 20f)
                return new UnityEngine.Color(1f, 0.5f, 0.2f); // Orange - low
            return new UnityEngine.Color(0.7f, 0.7f, 0.7f); // Gray - very low
        }

        /// <summary>
        /// Removes any existing TAIGU tag from the text.
        /// </summary>
        private string RemoveExistingTag(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Match pattern like [XX 78%] or [XX] at the start
            int bracketEnd = text.IndexOf(']');
            if (bracketEnd > 0 && text.StartsWith("["))
            {
                string inner = text.Substring(1, bracketEnd - 1).Trim();
                // Check if it looks like our tag (2-4 letter code, optionally followed by percentage)
                if (inner.Length <= 10 && System.Text.RegularExpressions.Regex.IsMatch(inner, @"^[A-Z]{2,4}(\s+\d+%)?$"))
                {
                    // Remove the tag and any trailing space
                    text = text.Substring(bracketEnd + 1).TrimStart();
                }
            }

            return text;
        }
    }
}
