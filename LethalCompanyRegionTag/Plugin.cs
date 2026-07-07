using BepInEx;
using HarmonyLib;
using UnityEngine;
using PluginCfg = LethalCompanyRegionTag.Config.PluginConfig;

namespace LethalCompanyRegionTag
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "TAIGU.RoomRecognition";
        public const string PluginName = "TAIGU-Room recognition experiment";
        public const string PluginVersion = "1.1.0";

        public static Plugin Instance { get; private set; }
        internal static BepInEx.Logging.ManualLogSource LogSource => Instance.Logger;
        internal static Harmony HarmonyInstance { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Initialize configuration
            PluginCfg.Init(Config);

            // Apply Harmony patches
            HarmonyInstance = new Harmony(PluginGUID);
            try
            {
                HarmonyInstance.PatchAll(typeof(Patches.SteamLobbyManagerPatch));
                LogSource.LogInfo("[TAIGU] Successfully patched: SteamLobbyManagerPatch");
            }
            catch (System.Exception ex)
            {
                LogSource.LogError($"[TAIGU] Failed to apply SteamLobbyManager patches: {ex.Message}");
            }

            try
            {
                HarmonyInstance.PatchAll(typeof(Patches.LobbySlotPatch));
                LogSource.LogInfo("[TAIGU] Successfully patched: LobbySlotPatch");
            }
            catch (System.Exception ex)
            {
                LogSource.LogError($"[TAIGU] Failed to apply LobbySlot patches: {ex.Message}");
            }

            // Log startup info
            LogSource.LogInfo($"[TAIGU] {PluginName} v{PluginVersion} loaded!");
            LogSource.LogInfo($"[TAIGU] Region detection enabled: Nickname={PluginCfg.EnableNicknameAnalysis.Value}, SteamAPI={!string.IsNullOrEmpty(PluginCfg.SteamWebApiKey.Value)}");
            LogSource.LogInfo($"[TAIGU] Community query: {PluginCfg.EnableCommunityQuery.Value}, XML query: {PluginCfg.EnableXmlQuery.Value}");

            if (string.IsNullOrEmpty(PluginCfg.SteamWebApiKey.Value))
            {
                LogSource.LogWarning("[TAIGU] No Steam Web API key configured. Region detection will rely on nickname analysis + community page queries.");
                LogSource.LogWarning("[TAIGU] For best results, get a free API key from: https://steamcommunity.com/dev/apikey");
            }
        }
    }
}
