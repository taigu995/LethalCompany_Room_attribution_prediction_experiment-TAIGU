using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Represents the result of a multi-source region analysis.
    /// </summary>
    public class RegionResult
    {
        /// <summary>Most likely region name (e.g., "China", "Japan", "Russia")</summary>
        public string PrimaryRegion { get; set; } = "Unknown";

        /// <summary>Overall confidence percentage (0-100)</summary>
        public float Confidence { get; set; }

        /// <summary>Probability distribution across all possible regions</summary>
        public Dictionary<string, float> Probabilities { get; set; } = new Dictionary<string, float>();

        /// <summary>ISO 3166-1 alpha-2 country code (if available)</summary>
        public string CountryCode { get; set; }

        /// <summary>Primary data source used for this result</summary>
        public string Source { get; set; } = "None";

        /// <summary>The display name that was analyzed</summary>
        public string DisplayName { get; set; }

        /// <summary>Steam ID of the lobby owner</summary>
        public ulong SteamId64 { get; set; }

        /// <summary>Timestamp when this result was generated</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Whether this result is still being computed</summary>
        public bool IsPending { get; set; }

        /// <summary>Display tag for UI (e.g., "[CN] China 78%")</summary>
        public string DisplayTag
        {
            get
            {
                if (IsPending)
                    return "[...] Analyzing...";

                if (string.IsNullOrEmpty(PrimaryRegion) || PrimaryRegion == "Unknown")
                    return "[?] Unknown";

                string code = !string.IsNullOrEmpty(CountryCode) ? CountryCode : GetRegionCode(PrimaryRegion);
                return $"[{code}] {PrimaryRegion} {Confidence:F0}%";
            }
        }

        /// <summary>Detailed probability breakdown for tooltip</summary>
        public string DetailText
        {
            get
            {
                if (Probabilities == null || Probabilities.Count == 0)
                    return "No data available";

                var sorted = Probabilities.OrderByDescending(x => x.Value).Take(5);
                var lines = sorted.Select(x => $"  {x.Key}: {x.Value:F1}%");
                return $"Region Analysis ({Source}):\n" + string.Join("\n", lines);
            }
        }

        private static string GetRegionCode(string region)
        {
            switch (region)
            {
                case "China": return "CN";
                case "Japan": return "JP";
                case "Korea": return "KR";
                case "Russia": return "RU";
                case "Ukraine": return "UA";
                case "Thailand": return "TH";
                case "India": return "IN";
                case "Vietnam": return "VN";
                case "Turkey": return "TR";
                case "Brazil": return "BR";
                case "Germany": return "DE";
                case "France": return "FR";
                case "Greece": return "GR";
                case "Israel": return "IL";
                case "Armenia": return "AM";
                case "Georgia": return "GE";
                case "Poland": return "PL";
                case "Nordic": return "SE";
                case "Middle East": return "SA";
                case "North Africa": return "EG";
                case "Latin America": return "MX";
                case "North America": return "US";
                case "Western Europe": return "EU";
                case "Southeast Asia": return "SEA";
                case "Eastern Europe": return "EE";
                case "Oceania": return "AU";
                case "Other CIS": return "CIS";
                case "Balkans": return "BA";
                case "Spain": return "ES";
                case "Italy": return "IT";
                case "Portugal": return "PT";
                case "Austria": return "AT";
                case "Belgium": return "BE";
                case "Cyprus": return "CY";
                case "Nepal": return "NP";
                case "Central Asia": return "CA";
                default: return "?";
            }
        }
    }
}
