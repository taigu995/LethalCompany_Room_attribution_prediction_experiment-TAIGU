using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Queries Steam services for user country/region information.
    /// Uses multiple methods with fallback chain.
    /// </summary>
    public static class SteamWebQuery
    {
        private static HttpClient _httpClient;

        private static HttpClient GetClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(8);
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) LethalCompanyRegionTag/1.0");
            }
            return _httpClient;
        }

        /// <summary>
        /// Method 1: Steam Web API (requires API key, most reliable).
        /// GET https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/
        /// </summary>
        public static async Task<string> GetCountryFromWebApi(string steamId64, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId64}";
                var response = await GetClient().GetStringAsync(url);

                // Parse loccountrycode from JSON response
                var match = Regex.Match(response, @"""loccountrycode""\s*:\s*""([^""]+)""");
                if (match.Success)
                    return match.Groups[1].Value.ToUpper();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Steam Web API query failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Method 2: Steam Community profile page (no API key needed).
        /// Parses the flag image from the profile page HTML.
        /// </summary>
        public static async Task<string> GetCountryFromCommunityPage(string steamId64)
        {
            if (string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                var url = $"https://steamcommunity.com/profiles/{steamId64}/";
                var response = await GetClient().GetStringAsync(url);

                // Look for country flag: flags/XX.png
                var match = Regex.Match(response, @"flags/(\w{2})\.png");
                if (match.Success)
                    return match.Groups[1].Value.ToUpper();

                // Alternative: look for location data in page source
                match = Regex.Match(response, @"""locationcountrycode""\s*:\s*""([^""]+)""");
                if (match.Success)
                    return match.Groups[1].Value.ToUpper();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Steam Community query failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Method 3: Steam XML profile (fallback, no API key needed).
        /// GET https://steamcommunity.com/profiles/{steamId64}/?xml=1
        /// </summary>
        public static async Task<string> GetCountryFromXml(string steamId64)
        {
            if (string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                var url = $"https://steamcommunity.com/profiles/{steamId64}/?xml=1";
                var response = await GetClient().GetStringAsync(url);

                // Parse <locationcountrycode>XX</locationcountrycode>
                var match = Regex.Match(response, @"<locationcountrycode>([^<]+)</locationcountrycode>");
                if (match.Success)
                    return match.Groups[1].Value.Trim().ToUpper();

                // Alternative: parse <location> element
                match = Regex.Match(response, @"<location>([^<]+)</location>");
                if (match.Success)
                {
                    string location = match.Groups[1].Value.Trim();
                    return TryExtractCountryFromLocation(location);
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] Steam XML query failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Combined query that tries all web sources in order.
        /// Returns a WebQueryResult with country code and source info.
        /// </summary>
        public static async Task<WebQueryResult> QueryAllSources(string steamId64)
        {
            var result = new WebQueryResult();

            if (string.IsNullOrEmpty(steamId64))
                return result;

            // Try Steam Web API first (if key configured)
            string apiKey = Config.PluginConfig.SteamWebApiKey.Value;
            if (!string.IsNullOrEmpty(apiKey))
            {
                string code = await GetCountryFromWebApi(steamId64, apiKey);
                if (!string.IsNullOrEmpty(code))
                {
                    result.CountryCode = code;
                    result.Source = "Steam Web API";
                    result.Confidence = 95f;
                    return result;
                }
            }

            // Try Steam Community page (no API key needed)
            if (Config.PluginConfig.EnableCommunityQuery.Value)
            {
                string code = await GetCountryFromCommunityPage(steamId64);
                if (!string.IsNullOrEmpty(code))
                {
                    result.CountryCode = code;
                    result.Source = "Steam Community";
                    result.Confidence = 88f;
                    return result;
                }
            }

            // Try Steam XML profile (fallback)
            if (Config.PluginConfig.EnableXmlQuery.Value)
            {
                string code = await GetCountryFromXml(steamId64);
                if (!string.IsNullOrEmpty(code))
                {
                    result.CountryCode = code;
                    result.Source = "Steam XML";
                    result.Confidence = 85f;
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Result from a web query.
        /// </summary>
        public class WebQueryResult
        {
            public string CountryCode;
            public string Source;
            public float Confidence;
        }

        /// <summary>
        /// Attempts to extract a country code from a free-form location string.
        /// </summary>
        private static string TryExtractCountryFromLocation(string location)
        {
            if (string.IsNullOrEmpty(location)) return null;
            location = location.ToLower();

            if (location.Contains("china") || location.Contains("中国")) return "CN";
            if (location.Contains("japan") || location.Contains("日本")) return "JP";
            if (location.Contains("korea") || location.Contains("한국")) return "KR";
            if (location.Contains("russia") || location.Contains("росс")) return "RU";
            if (location.Contains("ukraine") || location.Contains("україн")) return "UA";
            if (location.Contains("united states") || location.Contains("usa") || location.Contains("america")) return "US";
            if (location.Contains("germany") || location.Contains("deutschland")) return "DE";
            if (location.Contains("france")) return "FR";
            if (location.Contains("italy") || location.Contains("italia")) return "IT";
            if (location.Contains("spain") || location.Contains("españa")) return "ES";
            if (location.Contains("brazil") || location.Contains("brasil")) return "BR";
            if (location.Contains("australia")) return "AU";
            if (location.Contains("india")) return "IN";
            if (location.Contains("thailand") || location.Contains("ไทย")) return "TH";
            if (location.Contains("turkey") || location.Contains("türkiye")) return "TR";
            if (location.Contains("poland") || location.Contains("polska")) return "PL";
            if (location.Contains("indonesia")) return "ID";
            if (location.Contains("vietnam")) return "VN";
            if (location.Contains("philippines")) return "PH";
            if (location.Contains("malaysia")) return "MY";
            if (location.Contains("singapore")) return "SG";
            if (location.Contains("mexico") || location.Contains("méxico")) return "MX";
            if (location.Contains("argentina")) return "AR";
            if (location.Contains("colombia")) return "CO";
            if (location.Contains("chile")) return "CL";

            return null;
        }
    }
}
