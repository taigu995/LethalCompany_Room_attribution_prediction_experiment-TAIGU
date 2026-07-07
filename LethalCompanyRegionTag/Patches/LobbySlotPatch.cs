using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LethalCompanyRegionTag.Analysis;

namespace LethalCompanyRegionTag.Patches
{
    /// <summary>
    /// Patches for LobbySlot to add region tags to each lobby entry.
    /// </summary>
    [HarmonyPatch]
    public static class LobbySlotPatch
    {
        // Track which LobbySlots we've already tagged to avoid duplicates
        private static readonly HashSet<int> _taggedSlots = new HashSet<int>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Postfix for LobbySlot.Awake or Start to add region tag UI element.
        /// We patch the method that initializes the slot's display.
        /// </summary>
        [HarmonyPatch(typeof(LobbySlot), "Awake")]
        [HarmonyPostfix]
        public static void Postfix_Awake(LobbySlot __instance)
        {
            try
            {
                // Schedule region tag addition after a frame to ensure UI is fully initialized
                __instance.StartCoroutine(AddRegionTagDelayed(__instance));
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[TAIGU] Error in LobbySlot.Awake patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Alternative: Patch the method that sets lobby data on the slot.
        /// </summary>
        [HarmonyPatch(typeof(LobbySlot), "SetLobbyData")]
        [HarmonyPostfix]
        public static void Postfix_SetLobbyData(LobbySlot __instance)
        {
            try
            {
                __instance.StartCoroutine(AddRegionTagDelayed(__instance));
            }
            catch (Exception ex)
            {
                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogWarning($"[TAIGU] SetLobbyData patch error: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch the OnEnable method to re-apply tags when slot becomes visible.
        /// </summary>
        [HarmonyPatch(typeof(LobbySlot), "OnEnable")]
        [HarmonyPostfix]
        public static void Postfix_OnEnable(LobbySlot __instance)
        {
            try
            {
                __instance.StartCoroutine(AddRegionTagDelayed(__instance));
            }
            catch { }
        }

        private static IEnumerator AddRegionTagDelayed(LobbySlot slot)
        {
            // Wait a frame for UI to be fully initialized
            yield return null;
            yield return null;

            try
            {
                AddRegionTag(slot);
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[TAIGU] AddRegionTag error: {ex.Message}");
            }
        }

        private static void AddRegionTag(LobbySlot slot)
        {
            if (slot == null) return;

            int slotHash = slot.GetHashCode();

            // Extract lobby owner info from the slot
            var lobbyInfo = ExtractLobbyInfoFromSlot(slot);
            if (lobbyInfo == null || lobbyInfo.SteamId64 == 0)
                return;

            // Get or create the region tag text component
            var tagText = GetOrCreateTagText(slot);
            if (tagText == null)
                return;

            // Quick analysis first (nickname only)
            var quickResult = RegionAnalyzer.QuickAnalyze(lobbyInfo.SteamId64, lobbyInfo.Name);
            UpdateTagDisplay(tagText, quickResult);

            // Then trigger full async analysis
            _ = StartFullAnalysis(slot, tagText, lobbyInfo);
        }

        private static LobbyInfo ExtractLobbyInfoFromSlot(LobbySlot slot)
        {
            try
            {
                var slotType = slot.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var info = new LobbyInfo();

                // Try various field names for Steam ID
                string[] steamIdFields = { "steamId", "SteamId", "lobbyId", "hostSteamId",
                    "ownerSteamId", "lobbyOwnerId", "serverSteamId", "hostId" };

                foreach (var fieldName in steamIdFields)
                {
                    var field = slotType.GetField(fieldName, flags);
                    if (field != null)
                    {
                        var val = field.GetValue(slot);
                        if (val is ulong ul) { info.SteamId64 = ul; break; }
                        else if (val is long l) { info.SteamId64 = (ulong)l; break; }
                        else if (val is int i) { info.SteamId64 = (ulong)i; break; }
                        else
                        {
                            // Try Facepunch SteamId struct
                            var valueProp = val.GetType().GetProperty("Value", flags);
                            if (valueProp != null)
                            {
                                info.SteamId64 = (ulong)valueProp.GetValue(val);
                                break;
                            }
                        }
                    }
                }

                // Try to get lobby name / host name
                string[] nameFields = { "lobbyName", "serverName", "name", "hostName",
                    "displayName", "lobbyDisplayName" };

                foreach (var fieldName in nameFields)
                {
                    var field = slotType.GetField(fieldName, flags);
                    if (field != null)
                    {
                        info.Name = field.GetValue(slot)?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(info.Name)) break;
                    }

                    // Also try TMP_Text fields
                    var textProp = slotType.GetProperty(fieldName, flags);
                    if (textProp != null)
                    {
                        var textVal = textProp.GetValue(slot);
                        if (textVal is TMP_Text tmp)
                        {
                            info.Name = tmp.text ?? "";
                            if (!string.IsNullOrEmpty(info.Name)) break;
                        }
                    }
                }

                if (info.SteamId64 > 0)
                    return info;
            }
            catch { }

            return null;
        }

        private static TMP_Text GetOrCreateTagText(LobbySlot slot)
        {
            try
            {
                string tagObjectName = "TAIGU_RegionTag";
                Transform existing = slot.transform.Find(tagObjectName);

                if (existing != null)
                {
                    var text = existing.GetComponent<TMP_Text>();
                    if (text != null) return text;
                }

                // Create a new TMP_Text for the region tag
                // Find an existing text element to use as a template
                var existingTexts = slot.GetComponentsInChildren<TMP_Text>(true);
                TMP_Text templateText = existingTexts?.FirstOrDefault();

                // Create the tag text object
                var tagObj = new GameObject(tagObjectName);
                tagObj.transform.SetParent(slot.transform, false);

                var textComponent = tagObj.AddComponent<TextMeshProUGUI>();

                if (templateText != null)
                {
                    // Copy font and basic settings from template
                    textComponent.font = templateText.font;
                    textComponent.fontSize = Config.PluginConfig.TagFontSize.Value;
                    textComponent.alignment = TextAlignmentOptions.MidlineRight;
                    textComponent.enableAutoSizing = false;
                }
                else
                {
                    textComponent.fontSize = Config.PluginConfig.TagFontSize.Value;
                    textComponent.alignment = TextAlignmentOptions.MidlineRight;
                }

                // Position the tag on the right side of the slot
                var rectTransform = tagObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Anchor to the right side
                    rectTransform.anchorMin = new Vector2(0.75f, 0f);
                    rectTransform.anchorMax = new Vector2(1f, 1f);
                    rectTransform.offsetMin = new Vector2(5f, 2f);
                    rectTransform.offsetMax = new Vector2(-5f, -2f);
                }

                // Add a layout element to prevent overlapping
                var layoutElement = tagObj.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 180f;
                layoutElement.flexibleWidth = 0f;

                return textComponent;
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[TAIGU] GetOrCreateTagText error: {ex.Message}");
                return null;
            }
        }

        private static void UpdateTagDisplay(TMP_Text tagText, RegionResult result)
        {
            if (tagText == null || result == null) return;

            if (result.IsPending)
            {
                tagText.text = "<color=#888888>[...] Analyzing...</color>";
                tagText.gameObject.SetActive(true);
                return;
            }

            if (string.IsNullOrEmpty(result.PrimaryRegion) || result.PrimaryRegion == "Unknown")
            {
                tagText.text = "<color=#666666>[?] Unknown</color>";
                tagText.gameObject.SetActive(Config.PluginConfig.ShowRegionTag.Value);
                return;
            }

            string colorHex = GetRegionColor(result.PrimaryRegion);
            string code = "";
            if (Config.PluginConfig.ShowCountryCode.Value && !string.IsNullOrEmpty(result.CountryCode))
                code = result.CountryCode;
            else if (!string.IsNullOrEmpty(result.PrimaryRegion))
                code = result.PrimaryRegion.Length > 3 ? result.PrimaryRegion.Substring(0, 3) : result.PrimaryRegion;

            string probText = "";
            if (Config.PluginConfig.ShowProbability.Value)
                probText = $" {result.Confidence:F0}%";

            tagText.text = $"<color={colorHex}>[{code}]{probText}</color>";
            tagText.gameObject.SetActive(Config.PluginConfig.ShowRegionTag.Value);
        }

        private static async System.Threading.Tasks.Task StartFullAnalysis(LobbySlot slot, TMP_Text tagText, LobbyInfo lobbyInfo)
        {
            try
            {
                // Wait a bit to avoid flooding Steam API
                await System.Threading.Tasks.Task.Delay(500);

                var result = await RegionAnalyzer.FullAnalyze(lobbyInfo.SteamId64, lobbyInfo.Name);

                // Update UI on main thread
                UnityMainThread.Enqueue(() =>
                {
                    if (tagText != null && slot != null)
                        UpdateTagDisplay(tagText, result);
                });
            }
            catch (Exception ex)
            {
                if (Config.PluginConfig.DebugLogging.Value)
                    Plugin.LogSource?.LogWarning($"[TAIGU] FullAnalysis error: {ex.Message}");
            }
        }

        private static string GetRegionColor(string region)
        {
            if (string.IsNullOrEmpty(region)) return Config.PluginConfig.TagColorDefault.Value;

            if (region.Contains("China")) return Config.PluginConfig.TagColorChina.Value;
            if (region == "Japan") return Config.PluginConfig.TagColorJapan.Value;
            if (region == "Korea") return Config.PluginConfig.TagColorKorea.Value;
            if (region == "Russia" || region.Contains("CIS") || region.Contains("Ukraine") || region.Contains("Belarus"))
                return Config.PluginConfig.TagColorRussia.Value;

            // Additional region colors
            switch (region)
            {
                case "Southeast Asia":
                case "Thailand":
                case "Vietnam":
                    return "#22AA44";
                case "India":
                case "Pakistan":
                case "Bangladesh":
                    return "#FF9933";
                case "Middle East":
                case "North Africa":
                    return "#00AA00";
                case "Turkey":
                    return "#E30A17";
                case "Brazil":
                case "Latin America":
                case "South America":
                    return "#009C3B";
                case "Germany":
                case "Austria":
                    return "#FFCC00";
                case "France":
                    return "#0055A4";
                case "UK/Ireland":
                    return "#003078";
                case "Italy":
                    return "#009246";
                case "Spain":
                case "Iberia":
                    return "#AA1518";
                case "Nordic":
                    return "#0066CC";
                case "Poland":
                    return "#DC143C";
                case "Greece":
                    return "#0D5EAF";
                case "North America":
                    return "#3C3B6E";
                case "Oceania":
                    return "#00008B";
                case "Eastern Europe":
                case "Balkans":
                    return "#8B4513";
                case "Western Europe":
                    return "#4169E1";
                case "Africa":
                    return "#009639";
                default:
                    return Config.PluginConfig.TagColorDefault.Value;
            }
        }
    }

    /// <summary>
    /// Helper to enqueue actions on Unity's main thread.
    /// </summary>
    public class UnityMainThread : MonoBehaviour
    {
        private static UnityMainThread _instance;
        private static readonly Queue<Action> _actions = new Queue<Action>();
        private static readonly object _lock = new object();

        public static void Enqueue(Action action)
        {
            lock (_lock)
            {
                _actions.Enqueue(action);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            lock (_lock)
            {
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogSource?.LogError($"[TAIGU] Main thread action error: {ex.Message}");
                    }
                }
            }
        }

        public static void EnsureInitialized()
        {
            if (_instance != null) return;

            var go = new GameObject("TAIGU_MainThreadHelper");
            go.AddComponent<UnityMainThread>();
            DontDestroyOnLoad(go);
        }
    }
}
