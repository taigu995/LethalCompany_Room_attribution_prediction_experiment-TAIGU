using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Queries Steam Web API and Community pages to get user region info.
    /// </summary>
    public static class SteamWebQuery
    {
        private static HttpClient _httpClient;
        private static readonly object _lock = new object();

        private static HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    lock (_lock)
                    {
                        if (_httpClient == null)
                        {
                            var handler = new HttpClientHandler
                            {
                                AllowAutoRedirect = true,
                                UseCookies = false
                            };
                            _httpClient = new HttpClient(handler)
                            {
                                Timeout = TimeSpan.FromSeconds(8)
                            };
                            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                                "User-Agent",
                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        }
                    }
                }
                return _httpClient;
            }
        }

        /// <summary>
        /// Method 1: Query Steam Web API for user summary (requires API key).
        /// Returns the loccountrycode if available.
        /// </summary>
        public static async Task<SteamQueryResult> QuerySteamWebApi(string steamId64, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId64}";
                string response = await HttpClient.GetStringAsync(url);

                // Extract loccountrycode
                string countryCode = null;
                var countryMatch = Regex.Match(response, @"""loccountrycode""\s*:\s*""([^""]+)""");
                if (countryMatch.Success)
                    countryCode = countryMatch.Groups[1].Value.ToUpperInvariant();

                // Extract personaname
                string personaName = null;
                var nameMatch = Regex.Match(response, @"""personaname""\s*:\s*""([^""]+)""");
                if (nameMatch.Success)
                    personaName = nameMatch.Groups[1].Value;

                // Extract locstatecode (state/province)
                string stateCode = null;
                var stateMatch = Regex.Match(response, @"""locstatecode""\s*:\s*""([^""]+)""");
                if (stateMatch.Success)
                    stateCode = stateMatch.Groups[1].Value;

                return new SteamQueryResult
                {
                    CountryCode = countryCode,
                    PersonaName = personaName,
                    StateCode = stateCode,
                    Source = "Steam Web API",
                    Success = !string.IsNullOrEmpty(countryCode)
                };
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogWarning($"[TAIGU] Steam Web API query failed for {steamId64}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Method 2: Query Steam Community profile page (no API key needed).
        /// Parses the HTML to extract country flag/info.
        /// </summary>
        public static async Task<SteamQueryResult> QueryCommunityPage(string steamId64)
        {
            if (string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                string url = $"https://steamcommunity.com/profiles/{steamId64}/";
                string response = await HttpClient.GetStringAsync(url);

                // Look for country flag image pattern: flags/XX.png
                string countryCode = null;
                var flagMatch = Regex.Match(response, @"flags/(\w{2})\.png");
                if (flagMatch.Success)
                    countryCode = flagMatch.Groups[1].Value.ToUpperInvariant();

                // Alternative: look for locationcountrycode in page data
                if (string.IsNullOrEmpty(countryCode))
                {
                    var locationMatch = Regex.Match(response, @"""locationcountrycode""\s*:\s*""([^""]+)""");
                    if (locationMatch.Success)
                        countryCode = locationMatch.Groups[1].Value.ToUpperInvariant();
                }

                // Alternative: look for data-country attribute
                if (string.IsNullOrEmpty(countryCode))
                {
                    var dataCountryMatch = Regex.Match(response, @"data-country=""(\w{2})""");
                    if (dataCountryMatch.Success)
                        countryCode = dataCountryMatch.Groups[1].Value.ToUpperInvariant();
                }

                // Extract persona name from profile page
                string personaName = null;
                var nameMatch = Regex.Match(response, @"<bdi>([^<]+)</bdi>");
                if (nameMatch.Success)
                    personaName = nameMatch.Groups[1].Value;

                return new SteamQueryResult
                {
                    CountryCode = countryCode,
                    PersonaName = personaName,
                    Source = "Steam Community",
                    Success = !string.IsNullOrEmpty(countryCode)
                };
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogWarning($"[TAIGU] Community page query failed for {steamId64}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Method 3: Query Steam user's profile summary via XML endpoint.
        /// </summary>
        public static async Task<SteamQueryResult> QueryXmlProfile(string steamId64)
        {
            if (string.IsNullOrEmpty(steamId64))
                return null;

            try
            {
                string url = $"https://steamcommunity.com/profiles/{steamId64}/?xml=1";
                string response = await HttpClient.GetStringAsync(url);

                // Extract country code from XML
                string countryCode = null;
                var countryMatch = Regex.Match(response, @"<locationcountrycode>([^<]+)</locationcountrycode>");
                if (countryMatch.Success)
                    countryCode = countryMatch.Groups[1].Value.ToUpperInvariant();

                // Extract steam name
                string personaName = null;
                var nameMatch = Regex.Match(response, @"<steamID><!\[CDATA\[([^\]]+)\]\]></steamID>");
                if (nameMatch.Success)
                    personaName = nameMatch.Groups[1].Value;

                return new SteamQueryResult
                {
                    CountryCode = countryCode,
                    PersonaName = personaName,
                    Source = "Steam XML",
                    Success = !string.IsNullOrEmpty(countryCode)
                };
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogWarning($"[TAIGU] XML profile query failed for {steamId64}: {ex.Message}");
                return null;
            }
        }
    }

    public class SteamQueryResult
    {
        public string CountryCode { get; set; }
        public string PersonaName { get; set; }
        public string StateCode { get; set; }
        public string Source { get; set; }
        public bool Success { get; set; }
    }
}
