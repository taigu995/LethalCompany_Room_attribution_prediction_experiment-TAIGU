using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Analyzes a player's display name to infer likely region/country
    /// based on Unicode character set distribution.
    /// </summary>
    public static class NicknameAnalyzer
    {
        // Unicode block definitions
        private static readonly Regex CJKUnified = new Regex(@"[\u4E00-\u9FFF\u3400-\u4DBF]");
        private static readonly Regex Hiragana = new Regex(@"[\u3040-\u309F]");
        private static readonly Regex Katakana = new Regex(@"[\u30A0-\u30FF\u31F0-\u31FF]");
        private static readonly Regex HangulSyllables = new Regex(@"[\uAC00-\uD7AF]");
        private static readonly Regex HangulJamo = new Regex(@"[\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uD7B0-\uD7FF]");
        private static readonly Regex Cyrillic = new Regex(@"[\u0400-\u04FF\u0500-\u052F\u2DE0-\u2DFF\uA640-\uA69F]");
        private static readonly Regex Arabic = new Regex(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]");
        private static readonly Regex Thai = new Regex(@"[\u0E00-\u0E7F]");
        private static readonly Regex Devanagari = new Regex(@"[\u0900-\u097F]");
        private static readonly Regex Greek = new Regex(@"[\u0370-\u03FF\u1F00-\u1FFF]");
        private static readonly Regex Hebrew = new Regex(@"[\u0590-\u05FF\uFB1D-\uFB4F]");
        private static readonly Regex Armenian = new Regex(@"[\u0530-\u058F\uFB00-\uFB17]");
        private static readonly Regex Georgian = new Regex(@"[\u10A0-\u10FF\u2D00-\u2D2F]");
        private static readonly Regex Latin = new Regex(@"[A-Za-z\u00C0-\u024F]");
        private static readonly Regex LatinExtended = new Regex(@"[\u0100-\u024F\u1E00-\u1EFF]");

        // Common name patterns for Latin-script languages
        private static readonly Regex SpanishPattern = new Regex(@"\b(juan|maria|jose|carlos|luis|pedro|pablo|diego|fernando|alejandro|antonio|manuel|francisco|rafael|sergio|miguel|angel|enrique|roberto|alberto|eduardo|andres|ricardo|jorge|oscar|ramon|victor|hernandez|garcia|rodriguez|martinez|lopez|gonzalez|perez|sanchez|ramirez|torres|flores|rivera|gomez|diaz|cruz|morales|reyes|gutierrez|ortiz|moreno|romero|alvarez|ruiz|jimenez|dominguez|fernandez|vasquez|mendez|herrera|vargas|castro|campos|medina|pena|delgado|vega|silva|espinoza|valdez|aguilar|santos|acosta|figueroa|cabrera|enriquez|miranda|maldonado|estrada|rojas|abreu|cervantes|santiago|velasquez|montoya|peralta|dominguez|soler|camacho|pimentel|de\s+la\s+cruz|de\s+jesus)\b", RegexOptions.IgnoreCase);
        private static readonly Regex GermanPattern = new Regex(@"\b(hans|klaus|peter|stefan|thomas|andreas|michael|markus|martin|bernd|jurgen|wolfgang|dieter|ulf|sven|lars|bjorn|fischer|muller|schmidt|schneider|wagner|weber|becker|hoffmann|schwarz|wolf|klein|neumann|hartmann|kruger|ludwig|lang|graf|heinrich)\b", RegexOptions.IgnoreCase);
        private static readonly Regex FrenchPattern = new Regex(@"\b(jean|pierre|jacques|philippe|alain|michel|louis|christophe|nicolas|julien|thomas|alexandre|olivier|laurent|stephane|frederic|dupont|martin|bernard|thomas|robert|petit|richard|durand|leroy|moreau|simon|laurent|lefebvre|michel|garcia|david|bertrand|roux|vincent|fournier|morel|girard|andre|lemercier|blanc|robin|clement|bonnet|picard|garnier|marie|renaud|chevalier|francois|faure|perrin|andre|merceron)\b", RegexOptions.IgnoreCase);
        private static readonly Regex VietnamesePattern = new Regex(@"\b(nguyen|tran|le|pham|hoang|huynh|phan|vu|vo|dang|bui|do|ho|ngo|duong|ly|thi|van|dung|hung|cuong|minh|tuan|hai|phong|linh|hoa|thanh|trang|lan|hien|mai|tuan|anh|duc|nhan|tam|tin|khoa|bao|chau|quoc|phu|thao|my|ngoc|trinh|an|binh|cong|dat|dien|gia|hanh|khanh|long|nam|quan|son|tai|thao|uyen|xuan|yen)\b", RegexOptions.IgnoreCase);

        public static NicknameAnalysisResult Analyze(string nickname)
        {
            var result = new NicknameAnalysisResult();

            if (string.IsNullOrWhiteSpace(nickname))
            {
                result.Probabilities["Unknown"] = 100f;
                result.PrimaryRegion = "Unknown";
                result.Confidence = 0f;
                result.AnalysisMethod = "Empty";
                return result;
            }

            // === Keyword-based detection (highest priority) ===
            string lowerName = nickname.ToLowerInvariant();
            
            // China keywords
            if (lowerName.Contains("china") || lowerName.Contains(" cn ") || lowerName.Contains("[cn]") ||
                lowerName.Contains("chinese") || lowerName.Contains("国服") || lowerName.Contains("中国") ||
                lowerName.Contains("中文") || lowerName.Contains("国人") || lowerName.Contains("no foreigner"))
            {
                result.Probabilities["China"] = 92f;
                result.Probabilities["Other"] = 8f;
                result.PrimaryRegion = "China";
                result.Confidence = 92f;
                result.AnalysisMethod = "Keyword: China";
                result.CountryCode = "CN";
                return result;
            }
            
            // Russia/CIS keywords
            if (lowerName.Contains("russia") || lowerName.Contains(" russian") || lowerName.Contains(" ru ") ||
                lowerName.Contains("[ru]") || lowerName.Contains("cis") || lowerName.Contains("рус"))
            {
                result.Probabilities["Russia"] = 85f;
                result.Probabilities["Other CIS"] = 10f;
                result.Probabilities["Other"] = 5f;
                result.PrimaryRegion = "Russia";
                result.Confidence = 85f;
                result.AnalysisMethod = "Keyword: Russia";
                result.CountryCode = "RU";
                return result;
            }
            
            // Korea keywords
            if (lowerName.Contains("korea") || lowerName.Contains(" korean") || lowerName.Contains(" kr ") ||
                lowerName.Contains("[kr]"))
            {
                result.Probabilities["Korea"] = 90f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Korea";
                result.Confidence = 90f;
                result.AnalysisMethod = "Keyword: Korea";
                result.CountryCode = "KR";
                return result;
            }
            
            // Japan keywords
            if (lowerName.Contains("japan") || lowerName.Contains(" japanese") || lowerName.Contains(" jp ") ||
                lowerName.Contains("[jp]"))
            {
                result.Probabilities["Japan"] = 90f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Japan";
                result.Confidence = 90f;
                result.AnalysisMethod = "Keyword: Japan";
                result.CountryCode = "JP";
                return result;
            }
            
            // Europe/English keywords
            if (lowerName.Contains("europe") || lowerName.Contains(" eu ") || lowerName.Contains("[eu]") ||
                lowerName.Contains("english") || lowerName.Contains("german") || lowerName.Contains("french"))
            {
                result.Probabilities["Europe"] = 70f;
                result.Probabilities["North America"] = 15f;
                result.Probabilities["Other"] = 15f;
                result.PrimaryRegion = "Europe";
                result.Confidence = 70f;
                result.AnalysisMethod = "Keyword: Europe";
                return result;
            }

            bool hasCJK = CJKUnified.IsMatch(nickname);
            bool hasHiragana = Hiragana.IsMatch(nickname);
            bool hasKatakana = Katakana.IsMatch(nickname);
            bool hasHangul = HangulSyllables.IsMatch(nickname) || HangulJamo.IsMatch(nickname);
            bool hasCyrillic = Cyrillic.IsMatch(nickname);
            bool hasArabic = Arabic.IsMatch(nickname);
            bool hasThai = Thai.IsMatch(nickname);
            bool hasDevanagari = Devanagari.IsMatch(nickname);
            bool hasGreek = Greek.IsMatch(nickname);
            bool hasHebrew = Hebrew.IsMatch(nickname);
            bool hasArmenian = Armenian.IsMatch(nickname);
            bool hasGeorgian = Georgian.IsMatch(nickname);
            bool hasLatin = Latin.IsMatch(nickname);

            // Count CJK characters for Chinese vs Japanese distinction
            int cjkCount = CJKUnified.Matches(nickname).Count;
            int hiraganaCount = Hiragana.Matches(nickname).Count;
            int katakanaCount = Katakana.Matches(nickname).Count;
            int hangulCount = HangulSyllables.Matches(nickname).Count + HangulJamo.Matches(nickname).Count;
            int totalNonLatin = cjkCount + hiraganaCount + katakanaCount + hangulCount;

            // === High-confidence detections ===

            // Japanese: Hiragana or Katakana is definitive
            if (hasHiragana || hasKatakana)
            {
                result.Probabilities["Japan"] = 92f;
                result.Probabilities["Other"] = 8f;
                result.PrimaryRegion = "Japan";
                result.Confidence = 92f;
                result.AnalysisMethod = "Kana detected";
                result.CountryCode = "JP";
                return result;
            }

            // Korean: Hangul is definitive
            if (hasHangul)
            {
                result.Probabilities["Korea"] = 92f;
                result.Probabilities["Other"] = 8f;
                result.PrimaryRegion = "Korea";
                result.Confidence = 92f;
                result.AnalysisMethod = "Hangul detected";
                result.CountryCode = "KR";
                return result;
            }

            // CJK without kana/hangul -> likely Chinese
            if (hasCJK && !hasHiragana && !hasKatakana && !hasHangul)
            {
                // Check if it might be Vietnamese (some Vietnamese names use CJK-like patterns)
                // But pure CJK is most likely Chinese
                result.Probabilities["China"] = 78f;
                result.Probabilities["Japan"] = 8f;
                result.Probabilities["Korea"] = 7f;
                result.Probabilities["Vietnam"] = 4f;
                result.Probabilities["Other"] = 3f;
                result.PrimaryRegion = "China";
                result.Confidence = 78f;
                result.AnalysisMethod = "CJK Hanzi detected";
                result.CountryCode = "CN";
                return result;
            }

            // Cyrillic -> Russia/CIS
            if (hasCyrillic)
            {
                result.Probabilities["Russia"] = 50f;
                result.Probabilities["Ukraine"] = 15f;
                result.Probabilities["Belarus"] = 8f;
                result.Probabilities["Other CIS"] = 12f;
                result.Probabilities["Balkans"] = 5f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Russia";
                result.Confidence = 50f;
                result.AnalysisMethod = "Cyrillic detected";
                result.CountryCode = "RU";
                return result;
            }

            // Arabic -> Middle East / North Africa
            if (hasArabic)
            {
                result.Probabilities["Middle East"] = 55f;
                result.Probabilities["North Africa"] = 20f;
                result.Probabilities["Central Asia"] = 10f;
                result.Probabilities["Other"] = 15f;
                result.PrimaryRegion = "Middle East";
                result.Confidence = 55f;
                result.AnalysisMethod = "Arabic script detected";
                result.CountryCode = "SA";
                return result;
            }

            // Thai -> Thailand
            if (hasThai)
            {
                result.Probabilities["Thailand"] = 88f;
                result.Probabilities["Other"] = 12f;
                result.PrimaryRegion = "Thailand";
                result.Confidence = 88f;
                result.AnalysisMethod = "Thai script detected";
                result.CountryCode = "TH";
                return result;
            }

            // Devanagari -> India
            if (hasDevanagari)
            {
                result.Probabilities["India"] = 85f;
                result.Probabilities["Nepal"] = 8f;
                result.Probabilities["Other"] = 7f;
                result.PrimaryRegion = "India";
                result.Confidence = 85f;
                result.AnalysisMethod = "Devanagari detected";
                result.CountryCode = "IN";
                return result;
            }

            // Greek -> Greece
            if (hasGreek)
            {
                result.Probabilities["Greece"] = 82f;
                result.Probabilities["Cyprus"] = 8f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Greece";
                result.Confidence = 82f;
                result.AnalysisMethod = "Greek script detected";
                result.CountryCode = "GR";
                return result;
            }

            // Hebrew -> Israel
            if (hasHebrew)
            {
                result.Probabilities["Israel"] = 85f;
                result.Probabilities["Other"] = 15f;
                result.PrimaryRegion = "Israel";
                result.Confidence = 85f;
                result.AnalysisMethod = "Hebrew script detected";
                result.CountryCode = "IL";
                return result;
            }

            // Armenian -> Armenia
            if (hasArmenian)
            {
                result.Probabilities["Armenia"] = 80f;
                result.Probabilities["Other"] = 20f;
                result.PrimaryRegion = "Armenia";
                result.Confidence = 80f;
                result.AnalysisMethod = "Armenian script detected";
                result.CountryCode = "AM";
                return result;
            }

            // Georgian -> Georgia
            if (hasGeorgian)
            {
                result.Probabilities["Georgia"] = 82f;
                result.Probabilities["Other"] = 18f;
                result.PrimaryRegion = "Georgia";
                result.Confidence = 82f;
                result.AnalysisMethod = "Georgian script detected";
                result.CountryCode = "GE";
                return result;
            }

            // === Latin script analysis (lower confidence) ===
            if (hasLatin)
            {
                // Try to detect specific language patterns
                bool isSpanish = SpanishPattern.IsMatch(nickname);
                bool isGerman = GermanPattern.IsMatch(nickname);
                bool isFrench = FrenchPattern.IsMatch(nickname);
                bool isVietnamese = VietnamesePattern.IsMatch(nickname);

                // Check for Vietnamese diacritics
                bool hasVietnameseDiacritics = Regex.IsMatch(nickname, @"[\u0103\u00E2\u00EA\u00F4\u01A1\u01B0\u0111\u00E0\u00E1\u00E3\u00E8\u00E9\u00EC\u00ED\u00F2\u00F3\u00F9\u00FA\u00FD\u1EA3\u1EA1\u1EB9\u1EC7\u1ECF\u1ED3\u1EDD\u1EE5\u1EEF]");

                // Check for Portuguese patterns
                bool hasPortuguese = Regex.IsMatch(nickname, @"[\u00E3\u00F5\u00E7\u00E1\u00E9\u00ED\u00F3\u00FA]", RegexOptions.IgnoreCase);

                // Check for Turkish patterns
                bool hasTurkish = Regex.IsMatch(nickname, @"[\u00E7\u011F\u0131\u00F6\u015F\u00FC]", RegexOptions.IgnoreCase);

                // Check for Polish/Slavic patterns
                bool hasPolish = Regex.IsMatch(nickname, @"[\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C]", RegexOptions.IgnoreCase);

                // Check for Scandinavian patterns
                bool hasScandinavian = Regex.IsMatch(nickname, @"[\u00E4\u00E5\u00F6\u00E6\u00F8]", RegexOptions.IgnoreCase);

                if (isVietnamese || hasVietnameseDiacritics)
                {
                    result.Probabilities["Vietnam"] = 75f;
                    result.Probabilities["Other"] = 25f;
                    result.PrimaryRegion = "Vietnam";
                    result.Confidence = 75f;
                    result.AnalysisMethod = "Vietnamese pattern";
                    result.CountryCode = "VN";
                    return result;
                }

                if (hasTurkish)
                {
                    result.Probabilities["Turkey"] = 72f;
                    result.Probabilities["Other"] = 28f;
                    result.PrimaryRegion = "Turkey";
                    result.Confidence = 72f;
                    result.AnalysisMethod = "Turkish characters";
                    result.CountryCode = "TR";
                    return result;
                }

                if (hasPolish)
                {
                    result.Probabilities["Poland"] = 65f;
                    result.Probabilities["Czech"] = 10f;
                    result.Probabilities["Other"] = 25f;
                    result.PrimaryRegion = "Poland";
                    result.Confidence = 65f;
                    result.AnalysisMethod = "Polish characters";
                    result.CountryCode = "PL";
                    return result;
                }

                if (hasScandinavian)
                {
                    result.Probabilities["Nordic"] = 55f;
                    result.Probabilities["Germany"] = 15f;
                    result.Probabilities["Other"] = 30f;
                    result.PrimaryRegion = "Nordic";
                    result.Confidence = 55f;
                    result.AnalysisMethod = "Scandinavian characters";
                    result.CountryCode = "SE";
                    return result;
                }

                if (isSpanish)
                {
                    result.Probabilities["Spain"] = 25f;
                    result.Probabilities["Latin America"] = 35f;
                    result.Probabilities["Other"] = 40f;
                    result.PrimaryRegion = "Latin America";
                    result.Confidence = 35f;
                    result.AnalysisMethod = "Spanish pattern";
                    result.CountryCode = "MX";
                    return result;
                }

                if (isGerman)
                {
                    result.Probabilities["Germany"] = 45f;
                    result.Probabilities["Austria"] = 10f;
                    result.Probabilities["Nordic"] = 10f;
                    result.Probabilities["Other"] = 35f;
                    result.PrimaryRegion = "Germany";
                    result.Confidence = 45f;
                    result.AnalysisMethod = "German pattern";
                    result.CountryCode = "DE";
                    return result;
                }

                if (isFrench)
                {
                    result.Probabilities["France"] = 40f;
                    result.Probabilities["Belgium"] = 8f;
                    result.Probabilities["Other"] = 52f;
                    result.PrimaryRegion = "France";
                    result.Confidence = 40f;
                    result.AnalysisMethod = "French pattern";
                    result.CountryCode = "FR";
                    return result;
                }

                if (hasPortuguese)
                {
                    result.Probabilities["Brazil"] = 45f;
                    result.Probabilities["Portugal"] = 20f;
                    result.Probabilities["Other"] = 35f;
                    result.PrimaryRegion = "Brazil";
                    result.Confidence = 45f;
                    result.AnalysisMethod = "Portuguese characters";
                    result.CountryCode = "BR";
                    return result;
                }

                // Generic Latin -> broad distribution
                result.Probabilities["Western Europe"] = 20f;
                result.Probabilities["North America"] = 20f;
                result.Probabilities["Latin America"] = 15f;
                result.Probabilities["Southeast Asia"] = 10f;
                result.Probabilities["Eastern Europe"] = 10f;
                result.Probabilities["Other"] = 25f;
                result.PrimaryRegion = "Western Europe";
                result.Confidence = 20f;
                result.AnalysisMethod = "Latin script (generic)";
                result.CountryCode = null;
                return result;
            }

            // Emoji or other special characters
            result.Probabilities["Unknown"] = 100f;
            result.PrimaryRegion = "Unknown";
            result.Confidence = 0f;
            result.AnalysisMethod = "Unrecognizable";
            return result;
        }
    }

    public class NicknameAnalysisResult
    {
        public Dictionary<string, float> Probabilities { get; set; } = new Dictionary<string, float>();
        public string PrimaryRegion { get; set; } = "Unknown";
        public float Confidence { get; set; }
        public string AnalysisMethod { get; set; }
        public string CountryCode { get; set; }
    }
}
