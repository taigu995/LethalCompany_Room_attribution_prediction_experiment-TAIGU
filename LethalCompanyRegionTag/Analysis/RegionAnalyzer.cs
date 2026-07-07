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
        /// Maps a 2-letter country code to a human-readable region name.
        /// </summary>
        public static string CountryCodeToRegion(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Unknown";
            code = code.ToUpper();

            switch (code)
            {
                case "CN": return "China";
                case "TW": case "HK": case "MO": return "China (TW/HK/MO)";
                case "JP": return "Japan";
                case "KR": return "Korea";
                case "US": case "CA": return "North America";
                case "GB": case "IE": return "UK/Ireland";
                case "DE": return "Germany";
                case "FR": return "France";
                case "IT": return "Italy";
                case "ES": case "PT": return "Iberia";
                case "RU": return "Russia";
                case "UA": return "Ukraine";
                case "PL": return "Poland";
                case "BR": return "Brazil";
                case "AU": case "NZ": return "Oceania";
                case "IN": return "India";
                case "TH": return "Thailand";
                case "ID": case "MY": case "PH": case "SG": case "VN": return "Southeast Asia";
                case "TR": return "Turkey";
                case "SA": case "AE": case "EG": return "Middle East";
                case "MX": case "AR": case "CL": case "CO": return "Latin America";
                case "SE": case "NO": case "DK": case "FI": return "Nordic";
                case "NL": case "BE": return "Benelux";
                case "CZ": case "SK": case "HU": case "RO": case "BG": case "HR": case "RS": case "BA": case "SI": return "Eastern Europe";
                case "GR": return "Greece";
                case "IL": return "Israel";
                default: return code;
            }
        }

        /// <summary>
        /// Reverse mapping from region name to country code.
        /// </summary>
        public static string RegionToCountryCode(string region)
        {
            if (string.IsNullOrEmpty(region)) return null;

            if (region.Contains("China")) return "CN";
            if (region.Contains("Japan")) return "JP";
            if (region.Contains("Korea")) return "KR";
            if (region.Contains("Russia")) return "RU";
            if (region.Contains("Ukraine")) return "UA";
            if (region.Contains("North America")) return "US";
            if (region.Contains("UK")) return "GB";
            if (region.Contains("Germany")) return "DE";
            if (region.Contains("France")) return "FR";
            if (region.Contains("Italy")) return "IT";
            if (region.Contains("Iberia")) return "ES";
            if (region.Contains("Brazil")) return "BR";
            if (region.Contains("Oceania")) return "AU";
            if (region.Contains("India")) return "IN";
            if (region.Contains("Thailand")) return "TH";
            if (region.Contains("Southeast Asia")) return "ID";
            if (region.Contains("Turkey")) return "TR";
            if (region.Contains("Middle East")) return "SA";
            if (region.Contains("Latin America")) return "MX";
            if (region.Contains("Nordic")) return "SE";
            if (region.Contains("Benelux")) return "NL";
            if (region.Contains("Eastern Europe")) return "PL";
            if (region.Contains("Greece")) return "GR";
            if (region.Contains("Israel")) return "IL";
            if (region.Contains("Poland")) return "PL";

            return null;
        }

        private static Dictionary<string, float> BuildProbabilityMap(string primaryRegion, float primaryConfidence)
        {
            var map = new Dictionary<string, float>();
            map[primaryRegion] = primaryConfidence;
            float remaining = 100f - primaryConfidence;
            if (remaining > 0f)
                map["Other"] = remaining;
            return map;
        }
    }
}
