using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Multi-source region analysis engine.
    /// Combines Steam Web API data, community page data, and nickname analysis
    /// to produce a probability-weighted region assessment.
    /// </summary>
    public static class RegionAnalyzer
    {
        private static readonly Cache.RegionCache _cache = new Cache.RegionCache();

        /// <summary>
        /// Analyze the region of a lobby owner using multiple data sources.
        /// </summary>
        public static async Task<RegionResult> AnalyzeRegion(ulong steamId64, string displayName)
        {
            string steamIdStr = steamId64.ToString();

            // Check cache first
            if (_cache.TryGet(steamIdStr, out RegionResult cached))
            {
                // Update display name if changed
                if (!string.IsNullOrEmpty(displayName))
                    cached.DisplayName = displayName;
                return cached;
            }

            var result = new RegionResult
            {
                SteamId64 = steamId64,
                DisplayName = displayName,
                IsPending = true
            };

            // Start analysis in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var analysisResult = await PerformAnalysis(steamIdStr, displayName);
                    _cache.Set(steamIdStr, analysisResult);
                }
                catch (Exception ex)
                {
                    Plugin.LogSource?.LogError($"[TAIGU] Analysis error for {steamIdStr}: {ex.Message}");
                }
            });

            // Return pending result immediately
            _cache.Set(steamIdStr, result);
            return result;
        }

        /// <summary>
        /// Synchronous version for quick nickname-only analysis.
        /// </summary>
        public static RegionResult QuickAnalyze(ulong steamId64, string displayName)
        {
            string steamIdStr = steamId64.ToString();

            // Check cache
            if (_cache.TryGet(steamIdStr, out RegionResult cached) && !cached.IsPending)
                return cached;

            // Quick nickname-only analysis
            var nicknameResult = NicknameAnalyzer.Analyze(displayName);
            var result = new RegionResult
            {
                SteamId64 = steamId64,
                DisplayName = displayName,
                PrimaryRegion = nicknameResult.PrimaryRegion,
                Confidence = nicknameResult.Confidence,
                Probabilities = nicknameResult.Probabilities,
                CountryCode = nicknameResult.CountryCode,
                Source = "Nickname (quick)",
                IsPending = false
            };

            _cache.Set(steamIdStr, result);
            return result;
        }

        /// <summary>
        /// Trigger a full async analysis (Steam API + nickname) for a lobby owner.
        /// </summary>
        public static async Task<RegionResult> FullAnalyze(ulong steamId64, string displayName)
        {
            string steamIdStr = steamId64.ToString();

            // Check if we already have a complete result
            if (_cache.TryGet(steamIdStr, out RegionResult cached) && !cached.IsPending && cached.Source != "Nickname (quick)")
                return cached;

            var result = await PerformAnalysis(steamIdStr, displayName);
            _cache.Set(steamIdStr, result);
            return result;
        }

        private static async Task<RegionResult> PerformAnalysis(string steamIdStr, string displayName)
        {
            var result = new RegionResult
            {
                SteamId64 = ulong.TryParse(steamIdStr, out ulong id) ? id : 0,
                DisplayName = displayName
            };

            // === Source 1: Steam Web API (highest accuracy) ===
            string apiKey = Config.PluginConfig.SteamWebApiKey?.Value;
            SteamQueryResult webApiResult = null;

            if (!string.IsNullOrEmpty(apiKey))
            {
                webApiResult = await SteamWebQuery.QuerySteamWebApi(steamIdStr, apiKey);
                if (webApiResult?.Success == true)
                {
                    result.CountryCode = webApiResult.CountryCode;
                    result.Source = "Steam Web API";
                }
            }

            // === Source 2: Steam Community page (fallback) ===
            SteamQueryResult communityResult = null;
            if (result.CountryCode == null && Config.PluginConfig.EnableCommunityQuery?.Value == true)
            {
                communityResult = await SteamWebQuery.QueryCommunityPage(steamIdStr);
                if (communityResult?.Success == true)
                {
                    result.CountryCode = communityResult.CountryCode;
                    result.Source = "Steam Community";
                }
            }

            // === Source 3: Steam XML profile (another fallback) ===
            SteamQueryResult xmlResult = null;
            if (result.CountryCode == null && Config.PluginConfig.EnableXmlQuery?.Value == true)
            {
                xmlResult = await SteamWebQuery.QueryXmlProfile(steamIdStr);
                if (xmlResult?.Success == true)
                {
                    result.CountryCode = xmlResult.CountryCode;
                    result.Source = "Steam XML";
                }
            }

            // === Source 4: Nickname analysis (always available) ===
            string nameToAnalyze = displayName;

            // Use the name from Steam API if available and game name is empty
            if (string.IsNullOrEmpty(nameToAnalyze))
            {
                nameToAnalyze = webApiResult?.PersonaName
                    ?? communityResult?.PersonaName
                    ?? xmlResult?.PersonaName;
            }

            var nicknameResult = NicknameAnalyzer.Analyze(nameToAnalyze);

            // === Merge results ===
            if (!string.IsNullOrEmpty(result.CountryCode))
            {
                // We have a country code from Steam - high confidence
                string regionName = CountryCodeToRegion(result.CountryCode);
                result.PrimaryRegion = regionName;
                result.Confidence = 88f;

                // Build probability map: Steam data dominates
                result.Probabilities = new Dictionary<string, float>();
                result.Probabilities[regionName] = 88f;
                result.Probabilities["Other"] = 12f;

                // If nickname analysis agrees, boost confidence
                if (nicknameResult.PrimaryRegion == regionName ||
                    IsRegionMatch(result.CountryCode, nicknameResult.PrimaryRegion))
                {
                    result.Confidence = Math.Min(98f, result.Confidence + nicknameResult.Confidence * 0.1f);
                    result.Probabilities[regionName] = result.Confidence;
                    result.Probabilities["Other"] = 100f - result.Confidence;
                    result.Source += " + Nickname confirmed";
                }
            }
            else
            {
                // No Steam data - rely on nickname analysis
                result.Source = "Nickname Analysis";
                result.CountryCode = nicknameResult.CountryCode;
                result.PrimaryRegion = nicknameResult.PrimaryRegion;
                result.Confidence = nicknameResult.Confidence;
                result.Probabilities = nicknameResult.Probabilities;
            }

            result.IsPending = false;
            result.Timestamp = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// Check if a country code matches a region name from nickname analysis.
        /// </summary>
        private static bool IsRegionMatch(string countryCode, string regionName)
        {
            var mapping = new Dictionary<string, string[]>
            {
                { "CN", new[] { "China" } },
                { "TW", new[] { "China" } },
                { "HK", new[] { "China" } },
                { "JP", new[] { "Japan" } },
                { "KR", new[] { "Korea" } },
                { "RU", new[] { "Russia" } },
                { "UA", new[] { "Ukraine" } },
                { "TH", new[] { "Thailand" } },
                { "IN", new[] { "India" } },
                { "VN", new[] { "Vietnam" } },
                { "TR", new[] { "Turkey" } },
                { "BR", new[] { "Brazil" } },
                { "DE", new[] { "Germany" } },
                { "FR", new[] { "France" } },
                { "GR", new[] { "Greece" } },
                { "IL", new[] { "Israel" } },
                { "PL", new[] { "Poland" } },
                { "SE", new[] { "Nordic" } },
                { "NO", new[] { "Nordic" } },
                { "DK", new[] { "Nordic" } },
                { "FI", new[] { "Nordic" } },
            };

            if (mapping.TryGetValue(countryCode, out string[] regions))
                return regions.Contains(regionName);
            return false;
        }

        /// <summary>
        /// Map ISO country code to a human-readable region name.
        /// </summary>
        public static string CountryCodeToRegion(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Unknown";

            switch (code.ToUpperInvariant())
            {
                // East Asia
                case "CN": return "China";
                case "TW": case "HK": case "MO": return "China (TW/HK/MO)";
                case "JP": return "Japan";
                case "KR": return "Korea";
                case "MN": return "Mongolia";

                // Southeast Asia
                case "VN": return "Vietnam";
                case "TH": return "Thailand";
                case "ID": case "MY": case "PH": case "SG": return "Southeast Asia";
                case "MM": case "KH": case "LA": return "Southeast Asia";

                // South Asia
                case "IN": return "India";
                case "PK": return "Pakistan";
                case "BD": return "Bangladesh";
                case "NP": return "Nepal";
                case "LK": return "Sri Lanka";

                // North Asia / CIS
                case "RU": return "Russia";
                case "UA": return "Ukraine";
                case "BY": return "Belarus";
                case "KZ": case "UZ": case "TM": case "KG": case "TJ": return "Central Asia";
                case "GE": return "Georgia";
                case "AM": return "Armenia";
                case "AZ": return "Azerbaijan";
                case "MD": return "Moldova";

                // Middle East
                case "SA": case "AE": case "QA": case "KW": case "BH": case "OM": return "Middle East";
                case "IR": return "Iran";
                case "IQ": return "Iraq";
                case "SY": return "Syria";
                case "JO": return "Jordan";
                case "LB": return "Lebanon";
                case "IL": return "Israel";
                case "TR": return "Turkey";
                case "EG": return "North Africa";

                // North Africa
                case "MA": case "DZ": case "TN": case "LY": return "North Africa";

                // Sub-Saharan Africa
                case "NG": case "GH": case "KE": case "ZA": case "ET": return "Africa";
                case "TZ": case "UG": case "CM": case "CI": return "Africa";

                // Western Europe
                case "GB": case "IE": return "UK/Ireland";
                case "DE": return "Germany";
                case "FR": return "France";
                case "IT": return "Italy";
                case "ES": case "PT": return "Iberia";
                case "NL": case "BE": return "Benelux";
                case "CH": case "AT": return "Central Europe";
                case "LU": return "Luxembourg";

                // Nordic
                case "SE": case "NO": case "DK": case "FI": case "IS": return "Nordic";

                // Eastern Europe
                case "PL": return "Poland";
                case "CZ": case "SK": return "Czech/Slovakia";
                case "HU": return "Hungary";
                case "RO": case "BG": return "Romania/Bulgaria";
                case "HR": case "RS": case "BA": case "ME": case "MK": case "AL": return "Balkans";
                case "SI": return "Slovenia";
                case "GR": return "Greece";
                case "CY": return "Cyprus";

                // North America
                case "US": case "CA": return "North America";
                case "MX": return "Mexico";

                // Central America & Caribbean
                case "CR": case "PA": case "GT": case "HN": case "SV": case "NI": return "Central America";
                case "CU": case "DO": case "PR": case "JM": case "HT": return "Caribbean";

                // South America
                case "BR": return "Brazil";
                case "AR": case "CL": case "CO": case "PE": case "VE": return "South America";
                case "EC": case "BO": case "PY": case "UY": return "South America";

                // Oceania
                case "AU": case "NZ": return "Oceania";
                case "FJ": case "PG": return "Pacific Islands";

                default: return code; // Return raw code for unknown
            }
        }

        /// <summary>
        /// Get a short display tag for a country code.
        /// </summary>
        public static string GetShortTag(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) return "?";
            return countryCode.ToUpperInvariant();
        }
    }
}
