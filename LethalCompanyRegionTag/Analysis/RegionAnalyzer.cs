using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Multi-source region analysis engine.
    /// Combines Steam Web API queries, Steam Community page parsing,
    /// and nickname language analysis for comprehensive region detection.
    /// </summary>
    public static class RegionAnalyzer
    {
        /// <summary>
        /// Full async analysis using all available sources.
        /// </summary>
        public static async Task<RegionResult> AnalyzeRegion(string ownerName, ulong ownerSteamId)
        {
            var result = new RegionResult();

            if (string.IsNullOrEmpty(ownerName) && ownerSteamId == 0)
            {
                result.PrimaryRegion = "Unknown";
                result.Confidence = 0f;
                result.Source = "No data";
                return result;
            }

            // Source 1: Steam Web API (highest accuracy, needs API key)
            string apiKey = Config.PluginConfig.SteamWebApiKey.Value;
            string countryCode = null;

            if (!string.IsNullOrEmpty(apiKey) && ownerSteamId != 0)
            {
                try
                {
                    countryCode = await SteamWebQuery.GetCountryFromWebApi(ownerSteamId.ToString(), apiKey);
                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        result.Source = "Steam Web API";
                        result.CountryCode = countryCode;
                        result.PrimaryRegion = CountryCodeToRegion(countryCode);
                        result.Confidence = 95f;
                        result.Probabilities = BuildProbabilityMap(result.PrimaryRegion, 95f);
                        Cache.RegionCache.Set(ownerSteamId, result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] Steam Web API error: {ex.Message}");
                }
            }

            // Source 2: Steam Community page (no API key needed)
            if (Config.PluginConfig.EnableCommunityQuery.Value && ownerSteamId != 0)
            {
                try
                {
                    countryCode = await SteamWebQuery.GetCountryFromCommunityPage(ownerSteamId.ToString());
                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        result.Source = "Steam Community";
                        result.CountryCode = countryCode;
                        result.PrimaryRegion = CountryCodeToRegion(countryCode);
                        result.Confidence = 88f;
                        result.Probabilities = BuildProbabilityMap(result.PrimaryRegion, 88f);
                        Cache.RegionCache.Set(ownerSteamId, result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] Steam Community query error: {ex.Message}");
                }
            }

            // Source 3: Steam XML profile (fallback, no API key)
            if (Config.PluginConfig.EnableXmlQuery.Value && ownerSteamId != 0)
            {
                try
                {
                    countryCode = await SteamWebQuery.GetCountryFromXml(ownerSteamId.ToString());
                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        result.Source = "Steam XML";
                        result.CountryCode = countryCode;
                        result.PrimaryRegion = CountryCodeToRegion(countryCode);
                        result.Confidence = 85f;
                        result.Probabilities = BuildProbabilityMap(result.PrimaryRegion, 85f);
                        Cache.RegionCache.Set(ownerSteamId, result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] Steam XML query error: {ex.Message}");
                }
            }

            // Source 4: Nickname analysis (always available, fallback)
            if (Config.PluginConfig.EnableNicknameAnalysis.Value && !string.IsNullOrEmpty(ownerName))
            {
                var nicknameResult = NicknameAnalyzer.Analyze(ownerName);
                if (nicknameResult.Probabilities.Count > 0)
                {
                    result.PrimaryRegion = nicknameResult.PrimaryRegion;
                    result.Confidence = nicknameResult.Confidence;
                    result.Probabilities = nicknameResult.Probabilities;
                    result.Source = "Nickname Analysis";
                    result.CountryCode = nicknameResult.CountryCode ?? RegionToCountryCode(nicknameResult.PrimaryRegion);

                    Cache.RegionCache.Set(ownerSteamId, result);
                    return result;
                }
            }

            // No data available
            result.PrimaryRegion = "Unknown";
            result.Confidence = 0f;
            result.Source = "No data";
            result.Probabilities = new Dictionary<string, float> { { "Unknown", 100f } };
            return result;
        }

        /// <summary>
        /// Quick synchronous analysis using only nickname data (for timeouts).
        /// </summary>
        public static RegionResult GetQuickAnalysis(string ownerName)
        {
            var result = new RegionResult();

            if (!string.IsNullOrEmpty(ownerName))
            {
                var nicknameResult = NicknameAnalyzer.Analyze(ownerName);
                if (nicknameResult.Probabilities.Count > 0)
                {
                    result.PrimaryRegion = nicknameResult.PrimaryRegion;
                    result.Confidence = nicknameResult.Confidence;
                    result.Probabilities = nicknameResult.Probabilities;
                    result.Source = "Nickname Analysis (Quick)";
                    result.CountryCode = nicknameResult.CountryCode ?? RegionToCountryCode(nicknameResult.PrimaryRegion);
                    return result;
                }
            }

            result.PrimaryRegion = "Unknown";
            result.Confidence = 0f;
            result.Source = "No data";
            result.Probabilities = new Dictionary<string, float> { { "Unknown", 100f } };
            return result;
        }

        /// <summary>
        /// Maps a 2-letter ISO 3166-1 country code to a human-readable region name.
        /// Expanded to cover 80+ countries.
        /// </summary>
        public static string CountryCodeToRegion(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Unknown";
            code = code.ToUpper();

            switch (code)
            {
                // East Asia
                case "CN": return "China";
                case "TW": case "HK": case "MO": return "China (TW/HK/MO)";
                case "JP": return "Japan";
                case "KR": case "KP": return "Korea";
                case "MN": return "Mongolia";

                // Southeast Asia
                case "VN": return "Vietnam";
                case "TH": return "Thailand";
                case "ID": return "Indonesia";
                case "PH": return "Philippines";
                case "MY": return "Malaysia";
                case "SG": return "Singapore";
                case "KH": return "Cambodia";
                case "LA": return "Laos";
                case "MM": return "Myanmar";
                case "BN": return "Brunei";
                case "TL": return "Timor-Leste";

                // South Asia
                case "IN": return "India";
                case "PK": return "Pakistan";
                case "BD": return "Bangladesh";
                case "LK": return "Sri Lanka";
                case "NP": return "Nepal";
                case "BT": return "Bhutan";
                case "MV": return "Maldives";

                // Central Asia
                case "KZ": return "Kazakhstan";
                case "UZ": return "Uzbekistan";
                case "TM": return "Turkmenistan";
                case "KG": return "Kyrgyzstan";
                case "TJ": return "Tajikistan";

                // Middle East
                case "TR": return "Turkey";
                case "SA": return "Saudi Arabia";
                case "AE": return "UAE";
                case "IR": return "Iran";
                case "IQ": return "Iraq";
                case "IL": return "Israel";
                case "JO": return "Jordan";
                case "LB": return "Lebanon";
                case "SY": return "Syria";
                case "YE": return "Yemen";
                case "OM": return "Oman";
                case "QA": return "Qatar";
                case "KW": return "Kuwait";
                case "BH": return "Bahrain";
                case "CY": return "Cyprus";
                case "GE": return "Georgia";
                case "AM": return "Armenia";
                case "AZ": return "Azerbaijan";

                // North Africa
                case "EG": return "Egypt";
                case "MA": return "Morocco";
                case "DZ": return "Algeria";
                case "TN": return "Tunisia";
                case "LY": return "Libya";
                case "SD": return "Sudan";

                // Sub-Saharan Africa
                case "ZA": return "South Africa";
                case "NG": return "Nigeria";
                case "KE": return "Kenya";
                case "ET": return "Ethiopia";
                case "GH": return "Ghana";
                case "TZ": return "Tanzania";
                case "UG": return "Uganda";
                case "ER": return "Eritrea";

                // Russia / CIS / Eastern Europe
                case "RU": return "Russia";
                case "UA": return "Ukraine";
                case "BY": return "Belarus";
                case "MD": return "Moldova";

                // Central/Eastern Europe
                case "PL": return "Poland";
                case "CZ": return "Czech";
                case "SK": return "Slovakia";
                case "HU": return "Hungary";
                case "RO": return "Romania";
                case "BG": return "Bulgaria";
                case "HR": return "Croatia";
                case "RS": return "Serbia";
                case "BA": return "Bosnia";
                case "SI": return "Slovenia";
                case "MK": return "North Macedonia";
                case "AL": return "Albania";
                case "ME": return "Montenegro";
                case "XK": return "Kosovo";
                case "LT": case "LV": case "EE": return "Baltics";

                // Western Europe
                case "DE": return "Germany";
                case "AT": return "Austria";
                case "CH": return "Switzerland";
                case "FR": return "France";
                case "IT": return "Italy";
                case "ES": case "PT": return "Iberia";
                case "NL": case "BE": return "Benelux";
                case "GB": case "IE": return "UK/Ireland";
                case "LU": return "Luxembourg";
                case "MC": return "Monaco";

                // Nordic
                case "SE": return "Sweden";
                case "NO": return "Norway";
                case "DK": return "Denmark";
                case "FI": return "Finland";
                case "IS": return "Iceland";

                // Greece / Malta
                case "GR": return "Greece";
                case "MT": return "Malta";

                // North America
                case "US": case "CA": return "North America";

                // Latin America
                case "MX": return "Mexico";
                case "BR": return "Brazil";
                case "AR": return "Argentina";
                case "CL": return "Chile";
                case "CO": return "Colombia";
                case "PE": return "Peru";
                case "VE": return "Venezuela";
                case "EC": return "Ecuador";
                case "BO": return "Bolivia";
                case "PY": return "Paraguay";
                case "UY": return "Uruguay";
                case "PA": return "Panama";
                case "CR": return "Costa Rica";
                case "GT": return "Guatemala";
                case "HN": return "Honduras";
                case "SV": return "El Salvador";
                case "NI": return "Nicaragua";
                case "CU": return "Cuba";
                case "DO": return "Dominican Republic";
                case "PR": return "Puerto Rico";
                case "JM": return "Jamaica";
                case "TT": return "Trinidad";

                // Oceania
                case "AU": case "NZ": return "Oceania";
                case "FJ": return "Fiji";
                case "PG": return "Papua New Guinea";

                default: return code;
            }
        }

        /// <summary>
        /// Reverse mapping from region name to country code.
        /// </summary>
        public static string RegionToCountryCode(string region)
        {
            if (string.IsNullOrEmpty(region)) return null;

            switch (region)
            {
                case "China": return "CN";
                case "China (TW/HK/MO)": return "TW";
                case "Japan": return "JP";
                case "Korea": return "KR";
                case "Vietnam": return "VN";
                case "Thailand": return "TH";
                case "Indonesia": return "ID";
                case "Philippines": return "PH";
                case "Malaysia": return "MY";
                case "Singapore": return "SG";
                case "Cambodia": return "KH";
                case "Laos": return "LA";
                case "Myanmar": return "MM";
                case "India": return "IN";
                case "Pakistan": return "PK";
                case "Bangladesh": return "BD";
                case "Sri Lanka": return "LK";
                case "Russia": return "RU";
                case "Ukraine": return "UA";
                case "Belarus": return "BY";
                case "Kazakhstan": return "KZ";
                case "Turkey": return "TR";
                case "Saudi Arabia": return "SA";
                case "UAE": return "AE";
                case "Iran": return "IR";
                case "Egypt": return "EG";
                case "Israel": return "IL";
                case "Middle East": return "SA";
                case "North Africa": return "EG";
                case "Poland": return "PL";
                case "Czech": return "CZ";
                case "Slovakia": return "SK";
                case "Hungary": return "HU";
                case "Romania": return "RO";
                case "Bulgaria": return "BG";
                case "Croatia": return "HR";
                case "Serbia": return "RS";
                case "Greece": return "GR";
                case "Germany": return "DE";
                case "France": return "FR";
                case "Italy": return "IT";
                case "Iberia": return "ES";
                case "Benelux": return "NL";
                case "UK/Ireland": return "GB";
                case "Nordic": return "SE";
                case "Sweden": return "SE";
                case "Norway": return "NO";
                case "Denmark": return "DK";
                case "Finland": return "FI";
                case "North America": return "US";
                case "Brazil": return "BR";
                case "Mexico": return "MX";
                case "Argentina": return "AR";
                case "Latin America": return "MX";
                case "Oceania": return "AU";
                case "South Africa": return "ZA";
                case "Ethiopia": return "ET";
                case "Mongolia": return "MN";
                case "Georgia": return "GE";
                case "Armenia": return "AM";
                default: return null;
            }
        }

        /// <summary>
        /// Builds a probability distribution map from a primary region and confidence.
        /// </summary>
        public static Dictionary<string, float> BuildProbabilityMap(string primaryRegion, float confidence)
        {
            var map = new Dictionary<string, float>();
            map[primaryRegion] = confidence;
            float remaining = 100f - confidence;
            if (remaining > 0f)
                map["Other"] = remaining;
            return map;
        }
    }
}
