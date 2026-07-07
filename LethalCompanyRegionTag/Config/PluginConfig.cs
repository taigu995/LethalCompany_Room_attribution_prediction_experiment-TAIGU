using BepInEx.Configuration;

namespace LethalCompanyRegionTag.Config
{
    /// <summary>
    /// Plugin configuration entries managed through BepInEx config system.
    /// Config file is generated at: BepInEx/config/TAIGU.RoomRecognition.cfg
    /// </summary>
    public static class PluginConfig
    {
        // --- Analysis Sources ---
        public static ConfigEntry<bool> EnableNicknameAnalysis { get; private set; }
        public static ConfigEntry<bool> EnableCommunityQuery { get; private set; }
        public static ConfigEntry<bool> EnableXmlQuery { get; private set; }
        public static ConfigEntry<string> SteamWebApiKey { get; private set; }

        // --- Display Settings ---
        public static ConfigEntry<float> MinConfidenceThreshold { get; private set; }
        public static ConfigEntry<bool> ShowLowConfidenceTags { get; private set; }
        public static ConfigEntry<bool> ShowProbability { get; private set; }
        public static ConfigEntry<bool> UseChineseDisplay { get; private set; }

        public static void Init(ConfigFile configFile)
        {
            const string analysisSection = "Analysis Sources";
            const string displaySection = "Display Settings";

            EnableNicknameAnalysis = configFile.Bind(analysisSection,
                "EnableNicknameAnalysis", true,
                "Enable nickname language analysis (always works, no network needed). Analyzes Unicode character sets in lobby host names to detect language/region.");

            EnableCommunityQuery = configFile.Bind(analysisSection,
                "EnableCommunityQuery", true,
                "Enable Steam Community page query (no API key needed). Fetches the host's Steam profile page to extract country flag. May be slow or rate-limited.");

            EnableXmlQuery = configFile.Bind(analysisSection,
                "EnableXmlQuery", true,
                "Enable Steam XML profile query (fallback). Fetches the host's Steam profile in XML format to extract location info.");

            SteamWebApiKey = configFile.Bind(analysisSection,
                "SteamWebApiKey", "",
                "Steam Web API key for most accurate region detection. Get a free key from: https://steamcommunity.com/dev/apikey (requires Steam account). Leave empty to use other methods only.");

            MinConfidenceThreshold = configFile.Bind(displaySection,
                "MinConfidenceThreshold", 20f,
                new ConfigDescription(
                    "Minimum confidence percentage (0-100) required to show a region tag. Tags below this threshold are hidden.",
                    new AcceptableValueRange<float>(0f, 100f)));

            ShowLowConfidenceTags = configFile.Bind(displaySection,
                "ShowLowConfidenceTags", false,
                "Show [??] tag for lobbies where no region could be determined at all.");

            ShowProbability = configFile.Bind(displaySection,
                "ShowProbability", true,
                "Show probability percentage next to the region code (e.g. [CN 78%] instead of just [CN]).");

            UseChineseDisplay = configFile.Bind(displaySection,
                "UseChineseDisplay", true,
                "Use Chinese names for region display (e.g. [中国] instead of [CN]). Note: requires game font to support CJK characters. If you see garbled text, set this to false.");
        }
    }
}
