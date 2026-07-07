using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace LethalCompanyRegionTag
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "TAIGU.RoomRecognition";
        public const string PluginName = "TAIGU-Room recognition experiment";
        public const string PluginVersion = "1.0.0";

        public static Plugin Instance { get; private set; }
        internal static BepInEx.Logging.ManualLogSource LogSource => Instance?.Logger;
        internal static Harmony HarmonyInstance { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Initialize configuration
            LethalCompanyRegionTag.Config.PluginConfig.Init(this.Config);

            // Apply Harmony patches
            HarmonyInstance = new Harmony(PluginGUID);

            try
            {
                HarmonyInstance.PatchAll();
                Logger.LogInfo($"[TAIGU] {PluginName} v{PluginVersion} - Harmony patches applied successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[TAIGU] Failed to apply Harmony patches: {ex.Message}");
                Logger.LogError($"[TAIGU] Stack trace: {ex.StackTrace}");

                // Try partial patching - patch only the classes that exist
                try
                {
                    PatchAvailableTypes();
                }
                catch (System.Exception ex2)
                {
                    Logger.LogError($"[TAIGU] Partial patching also failed: {ex2.Message}");
                }
            }

            // Initialize main thread helper
            Patches.UnityMainThread.EnsureInitialized();

            Logger.LogInfo($"[TAIGU] Region detection enabled: Nickname={LethalCompanyRegionTag.Config.PluginConfig.EnableNicknameAnalysis.Value}, SteamAPI={LethalCompanyRegionTag.Config.PluginConfig.EnableSteamApiQuery.Value}");
            Logger.LogInfo($"[TAIGU] Community query: {LethalCompanyRegionTag.Config.PluginConfig.EnableCommunityQuery.Value}, XML query: {LethalCompanyRegionTag.Config.PluginConfig.EnableXmlQuery.Value}");

            if (string.IsNullOrEmpty(LethalCompanyRegionTag.Config.PluginConfig.SteamWebApiKey.Value))
            {
                Logger.LogWarning("[TAIGU] No Steam Web API key configured. Region detection will rely on nickname analysis only.");
                Logger.LogWarning("[TAIGU] For best results, get a free API key from: https://steamcommunity.com/dev/apikey");
            }
            else
            {
                Logger.LogInfo("[TAIGU] Steam Web API key configured - full region detection enabled!");
            }
        }

        /// <summary>
        /// Attempt to patch only the types that exist in the current game version.
        /// This provides graceful degradation if the game updates.
        /// </summary>
        private void PatchAvailableTypes()
        {
            var types = new System.Type[]
            {
                typeof(Patches.SteamLobbyManagerPatch),
                typeof(Patches.LobbySlotPatch)
            };

            foreach (var patchType in types)
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(patchType).Patch();
                    Logger.LogInfo($"[TAIGU] Successfully patched: {patchType.Name}");
                }
                catch (System.Exception ex)
                {
                    Logger.LogWarning($"[TAIGU] Could not patch {patchType.Name}: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            HarmonyInstance?.UnpatchSelf();
        }
    }
}
