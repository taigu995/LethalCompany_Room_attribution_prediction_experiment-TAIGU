using BepInEx.Configuration;

namespace LethalCompanyRegionTag.Config
{
    /// <summary>
    /// Plugin configuration managed through BepInEx config system.
    /// </summary>
    public static class PluginConfig
    {
        // === Steam API Settings ===
        public static ConfigEntry<string> SteamWebApiKey;
        public static ConfigEntry<bool> EnableCommunityQuery;
        public static ConfigEntry<bool> EnableXmlQuery;

        // === Display Settings ===
        public static ConfigEntry<bool> ShowRegionTag;
        public static ConfigEntry<bool> ShowProbability;
        public static ConfigEntry<bool> ShowCountryCode;
        public static ConfigEntry<float> TagFontSize;
        public static ConfigEntry<string> TagColorChina;
        public static ConfigEntry<string> TagColorJapan;
        public static ConfigEntry<string> TagColorKorea;
        public static ConfigEntry<string> TagColorRussia;
        public static ConfigEntry<string> TagColorDefault;

        // === Analysis Settings ===
        public static ConfigEntry<bool> EnableNicknameAnalysis;
        public static ConfigEntry<bool> EnableSteamApiQuery;
        public static ConfigEntry<float> CacheTtlMinutes;
        public static ConfigEntry<bool> AutoAnalyzeOnRefresh;

        // === Debug Settings ===
        public static ConfigEntry<bool> DebugLogging;

        public static void Init(ConfigFile config)
        {
            // Steam API
            SteamWebApiKey = config.Bind(
                "Steam API", "WebApiKey", "",
                "Your Steam Web API key (get one from https://steamcommunity.com/dev/apikey). Leave empty to disable Steam API queries.");

            EnableCommunityQuery = config.Bind(
                "Steam API", "EnableCommunityQuery", true,
                "Enable querying Steam Community profile pages for country info (no API key needed, but less reliable).");

            EnableXmlQuery = config.Bind(
                "Steam API", "EnableXmlQuery", true,
                "Enable querying Steam XML profile endpoint as fallback.");

            // Display
            ShowRegionTag = config.Bind(
                "Display", "ShowRegionTag", true,
                "Show region tag next to lobby entries.");

            ShowProbability = config.Bind(
                "Display", "ShowProbability", true,
                "Show probability percentage next to region tag.");

            ShowCountryCode = config.Bind(
                "Display", "ShowCountryCode", true,
                "Show ISO country code in the tag (e.g., [CN], [JP]).");

            TagFontSize = config.Bind(
                "Display", "TagFontSize", 14f,
                "Font size for region tags (10-24).");

            TagColorChina = config.Bind(
                "Display", "ColorChina", "#FF4444",
                "Color for China region tag (hex format).");

            TagColorJapan = config.Bind(
                "Display", "ColorJapan", "#FF69B4",
                "Color for Japan region tag (hex format).");

            TagColorKorea = config.Bind(
                "Display", "ColorKorea", "#4169E1",
                "Color for Korea region tag (hex format).");

            TagColorRussia = config.Bind(
                "Display", "ColorRussia", "#FF8C00",
                "Color for Russia/CIS region tag (hex format).");

            TagColorDefault = config.Bind(
                "Display", "ColorDefault", "#AAAAAA",
                "Default color for other region tags (hex format).");

            // Analysis
            EnableNicknameAnalysis = config.Bind(
                "Analysis", "EnableNicknameAnalysis", true,
                "Enable nickname language analysis for region detection.");

            EnableSteamApiQuery = config.Bind(
                "Analysis", "EnableSteamApiQuery", true,
                "Enable Steam API queries for region detection (requires API key or community query).");

            CacheTtlMinutes = config.Bind(
                "Analysis", "CacheTtlMinutes", 10f,
                "How long to cache region analysis results (in minutes).");

            AutoAnalyzeOnRefresh = config.Bind(
                "Analysis", "AutoAnalyzeOnRefresh", true,
                "Automatically analyze lobbies when the server list is refreshed.");

            // Debug
            DebugLogging = config.Bind(
                "Debug", "DebugLogging", false,
                "Enable detailed debug logging.");
        }
    }
}
