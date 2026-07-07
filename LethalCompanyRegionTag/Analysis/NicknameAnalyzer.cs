using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LethalCompanyRegionTag.Analysis
{
    /// <summary>
    /// Analyzes a player's display name / server name to infer likely region/country.
    /// Uses Unicode character set distribution, keyword matching, and Latin-script
    /// language pattern analysis.
    /// 
    /// Optimized v2: expanded keyword database (30+ countries), more Latin-script
    /// language patterns (15+ languages), additional Unicode script detection.
    /// </summary>
    public static class NicknameAnalyzer
    {
        // === Unicode block definitions ===
        private static readonly Regex CJKUnified = new Regex(@"[\u4E00-\u9FFF\u3400-\u4DBF]");
        private static readonly Regex Hiragana = new Regex(@"[\u3040-\u309F]");
        private static readonly Regex Katakana = new Regex(@"[\u30A0-\u30FF\u31F0-\u31FF]");
        private static readonly Regex HangulSyllables = new Regex(@"[\uAC00-\uD7AF]");
        private static readonly Regex HangulJamo = new Regex(@"[\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uD7B0-\uD7FF]");
        private static readonly Regex Cyrillic = new Regex(@"[\u0400-\u04FF\u0500-\u052F\u2DE0-\u2DFF\uA640-\uA69F]");
        private static readonly Regex Arabic = new Regex(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]");
        private static readonly Regex Thai = new Regex(@"[\u0E00-\u0E7F]");
        private static readonly Regex Lao = new Regex(@"[\u0E80-\u0EFF]");
        private static readonly Regex Khmer = new Regex(@"[\u1780-\u17FF\u19E0-\u19FF]");
        private static readonly Regex Myanmar = new Regex(@"[\u1000-\u109F\uAA60-\uAA7F]");
        private static readonly Regex Tibetan = new Regex(@"[\u0F00-\u0FFF]");
        private static readonly Regex Devanagari = new Regex(@"[\u0900-\u097F]");
        private static readonly Regex Bengali = new Regex(@"[\u0980-\u09FF]");
        private static readonly Regex Greek = new Regex(@"[\u0370-\u03FF\u1F00-\u1FFF]");
        private static readonly Regex Hebrew = new Regex(@"[\u0590-\u05FF\uFB1D-\uFB4F]");
        private static readonly Regex Armenian = new Regex(@"[\u0530-\u058F\uFB00-\uFB17]");
        private static readonly Regex Georgian = new Regex(@"[\u10A0-\u10FF\u2D00-\u2D2F]");
        private static readonly Regex Latin = new Regex(@"[A-Za-z\u00C0-\u024F]");
        private static readonly Regex LatinExtended = new Regex(@"[\u0100-\u024F\u1E00-\u1EFF]");
        private static readonly Regex Ethiopic = new Regex(@"[\u1200-\u137F\u1380-\u139F\u2D80-\u2DDF\uAB00-\uAB2F]");
        private static readonly Regex Sinhala = new Regex(@"[\u0D80-\u0DFF]");
        private static readonly Regex Tamil = new Regex(@"[\u0B80-\u0BFF]");
        private static readonly Regex Telugu = new Regex(@"[\u0C00-\u0C7F]");

        // === Keyword dictionaries (country code -> keywords) ===
        // Each entry: keywords that strongly indicate the room is from that country
        private static readonly Dictionary<string, string[]> CountryKeywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // East Asia
            ["CN"] = new[] { "china", " cn ", "[cn]", "chinese", "\u56fd\u670d", "\u4e2d\u56fd", "\u4e2d\u6587", "\u56fd\u4eba", "no foreigner", "\u4e0d\u8981\u5916\u56fd\u4eba", "\u534e\u4eba", "\u6c49\u8bed", "\u7b80\u4f53", "\u7e41\u4f53", "cn\u670d", "\u56fd\u9645\u670d\u7981\u6b62" },
            ["TW"] = new[] { " taiwan", " tw ", "[tw]", "\u53f0\u6e7e", "\u7e41\u4f53", "\u6b63\u9ad4\u5b57" },
            ["HK"] = new[] { " hong kong", " hk ", "[hk]", "\u9999\u6e2f", "cantonese" },
            ["JP"] = new[] { "japan", " japanese", " jp ", "[jp]", "\u65e5\u672c\u8a9e", "\u65e5\u672c\u4eba" },
            ["KR"] = new[] { "korea", " korean", " kr ", "[kr]", "\ud55c\uad6d\uc5b4", "\ud55c\uad6d\uc778" },

            // Southeast Asia
            ["VN"] = new[] { "vietnam", " vietnamese", " vn ", "[vn]", "tieng viet" },
            ["TH"] = new[] { "thailand", " thai ", " th ", "[th]", "\u0e44\u0e17\u0e22" },
            ["ID"] = new[] { "indonesia", " indonesian", " id ", "[id]", " indo ", "wibu", "orang indonesia" },
            ["PH"] = new[] { "philippines", " filipino", " ph ", "[ph]", " pinoy", " pinay", "tagalog" },
            ["MY"] = new[] { "malaysia", " malaysian", " my ", "[my]", "malaysian" },
            ["SG"] = new[] { "singapore", " sg ", "[sg]" },

            // South Asia
            ["IN"] = new[] { "india", " indian", " in ", "[in]", "hindi", "\u0939\u093f\u0928\u094d\u0926\u0940", "bharat" },
            ["PK"] = new[] { "pakistan", " pakistani", " pk ", "[pk]", "urdu" },
            ["BD"] = new[] { "bangladesh", " bangla", " bd ", "[bd]" },

            // Middle East
            ["SA"] = new[] { "saudi", " sa ", "[sa]", "\u0627\u0644\u0639\u0631\u0628\u064a\u0629" },
            ["AE"] = new[] { "uae", " emirates", " ae ", "[ae]", "dubai" },
            ["IR"] = new[] { "iran", " iranian", " ir ", "[ir]", "farsi", "\u0641\u0627\u0631\u0633\u06cc", "persian" },
            ["TR"] = new[] { "turkey", " turkish", " tr ", "[tr]", "t\u00fcrk\u00e7e", "t\u00fcrk", "tr\u5907" },

            // Russia / CIS
            ["RU"] = new[] { "russia", " russian", " ru ", "[ru]", "cis", "\u0440\u0443\u0441", "\u0440\u043e\u0441\u0441\u0438\u044f", "\u0440\u0443\u0441\u0441\u043a\u0438\u0439" },
            ["UA"] = new[] { "ukraine", " ukrainian", " ua ", "[ua]", "\u0443\u043a\u0440", "\u0443\u043a\u0440\u0430\u0457\u043d" },
            ["BY"] = new[] { "belarus", " belarusian", " by ", "[by]", "\u0431\u0435\u043b\u0430\u0440\u0443\u0441" },
            ["KZ"] = new[] { "kazakhstan", " kazakh", " kz ", "[kz]" },

            // Europe
            ["PL"] = new[] { "poland", " polish", " pl ", "[pl]", "polska", "polski", "polacy", "tylko polacy", "no foreigner pl" },
            ["DE"] = new[] { "germany", " german", " de ", "[de]", "deutsch", "deutsche", "nur deutsch" },
            ["FR"] = new[] { "france", " french", " fr ", "[fr]", "fran\u00e7ais", "francophone" },
            ["IT"] = new[] { "italy", " italian", " it ", "[it]", "italiano" },
            ["ES"] = new[] { "spain", " spanish", " es ", "[es]", "espa\u00f1ol", "espa\u00f1a", "hispano" },
            ["PT"] = new[] { "portugal", " portuguese", " pt ", "[pt]", "portugu\u00eas", "brasil", "brazil" },
            ["NL"] = new[] { "netherlands", " dutch", " nl ", "[nl]", "nederlands", "holland" },
            ["SE"] = new[] { "sweden", " swedish", " se ", "[se]", "svenska", "sverige" },
            ["NO"] = new[] { "norway", " norwegian", " no ", "[no]", "norsk", "norge" },
            ["DK"] = new[] { "denmark", " danish", " dk ", "[dk]", "dansk", "danmark" },
            ["FI"] = new[] { "finland", " finnish", " fi ", "[fi]", "suomi", "suomalainen" },
            ["CZ"] = new[] { "czech", " cz ", "[cz]", "\u010de\u0161tina", "\u010desk\u00fd" },
            ["SK"] = new[] { "slovakia", " slovak", " sk ", "[sk]", "sloven\u010dina" },
            ["HU"] = new[] { "hungary", " hungarian", " hu ", "[hu]", "magyar" },
            ["RO"] = new[] { "romania", " romanian", " ro ", "[ro]", "rom\u00e2n\u0103" },
            ["BG"] = new[] { "bulgaria", " bulgarian", " bg ", "[bg]", "\u0431\u044a\u043b\u0433\u0430\u0440" },
            ["HR"] = new[] { "croatia", " croatian", " hr ", "[hr]", "hrvatski" },
            ["RS"] = new[] { "serbia", " serbian", " rs ", "[rs]", "srpski", "\u0441\u0440\u043f\u0441\u043a\u0438" },
            ["GR"] = new[] { "greece", " greek", " gr ", "[gr]", "\u03b5\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03ac" },

            // Americas
            ["US"] = new[] { "usa", " us ", "[us]", "america", "american", "english only", "en only", "north america" },
            ["CA"] = new[] { "canada", " canadian", " ca ", "[ca]" },
            ["BR"] = new[] { "brazil", " brazilian", " br ", "[br]", "portugu\u00eas", "brasileiro" },
            ["MX"] = new[] { "mexico", " mexican", " mx ", "[mx]", "m\u00e9xico", "latino" },
            ["AR"] = new[] { "argentina", " argentinian", " ar ", "[ar]" },
            ["CL"] = new[] { "chile", " chilean", " cl ", "[cl]" },
            ["CO"] = new[] { "colombia", " colombian", " co ", "[co]" },

            // Oceania
            ["AU"] = new[] { "australia", " aussie", " au ", "[au]", "aussi" },
            ["NZ"] = new[] { "new zealand", " nz ", "[nz]", "kiwi" },

            // Africa
            ["EG"] = new[] { "egypt", " egyptian", " eg ", "[eg]", "\u0645\u0635\u0631" },
            ["ZA"] = new[] { "south africa", " za ", "[za]" },
            ["MA"] = new[] { "morocco", " moroccan", " ma ", "[ma]", "maroc" },
            ["DZ"] = new[] { "algeria", " algerian", " dz ", "[dz]", "alg\u00e9rie" },
        };

        // === Latin-script name patterns ===
        private static readonly Regex SpanishPattern = new Regex(@"\b(juan|maria|jose|carlos|luis|pedro|pablo|diego|fernando|alejandro|antonio|manuel|francisco|rafael|sergio|miguel|angel|enrique|roberto|alberto|eduardo|andres|ricardo|jorge|oscar|ramon|victor|hernandez|garcia|rodriguez|martinez|lopez|gonzalez|perez|sanchez|ramirez|torres|flores|rivera|gomez|diaz|cruz|morales|reyes|gutierrez|ortiz|moreno|romero|alvarez|ruiz|jimenez|dominguez|fernandez|vasquez|mendez|herrera|vargas|castro|campos|medina|pena|delgado|vega|silva|espinoza|valdez|aguilar|santos|acosta|figueroa|cabrera|enriquez|miranda|maldonado|estrada|rojas|abreu|cervantes|santiago|velasquez|montoya|peralta|soler|camacho|pimentel|de\s+la\s+cruz|de\s+jesus)\b", RegexOptions.IgnoreCase);
        private static readonly Regex GermanPattern = new Regex(@"\b(hans|klaus|peter|stefan|thomas|andreas|michael|markus|martin|bernd|jurgen|wolfgang|dieter|ulf|sven|lars|bjorn|fischer|muller|schmidt|schneider|wagner|weber|becker|hoffmann|schwarz|wolf|klein|neumann|hartmann|kruger|ludwig|lang|graf|heinrich)\b", RegexOptions.IgnoreCase);
        private static readonly Regex FrenchPattern = new Regex(@"\b(jean|pierre|jacques|philippe|alain|michel|louis|christophe|nicolas|julien|thomas|alexandre|olivier|laurent|stephane|frederic|dupont|bernard|robert|petit|richard|durand|leroy|moreau|simon|lefebvre|bertrand|roux|vincent|fournier|morel|girard|lemercier|blanc|robin|clement|bonnet|picard|garnier|marie|renaud|chevalier|francois|faure|perrin|merceron)\b", RegexOptions.IgnoreCase);
        private static readonly Regex VietnamesePattern = new Regex(@"\b(nguyen|tran|le\s|pham|hoang|huynh|phan|vu\s|vo\s|dang|bui|do\s|ho\s|ngo\s|duong|ly\s|thi\s|van\s|dung\s|hung\s|cuong|minh\s|tuan\s|hai\s|phong\s|linh\s|hoa\s|thanh\s|trang\s|lan\s|hien\s|mai\s|anh\s|duc\s|nhan\s|tam\s|tin\s|khoa\s|bao\s|chau\s|quoc\s|phu\s|thao\s|my\s|ngoc\s|trinh\s|an\s|binh\s|cong\s|dat\s|dien\s|gia\s|hanh\s|khanh\s|long\s|nam\s|quan\s|son\s|tai\s|uyen\s|xuan\s|yen\s)\b", RegexOptions.IgnoreCase);
        private static readonly Regex IndonesianPattern = new Regex(@"\b(surya|budi|andi|dewi|siti|agus|eko|rizky|fitri|novi|yuni|ari|dimas|fajar|galih|heru|irfan|joko|kurnia|lutfi|maya|nanda|oki|putri|rani|sari|tomi|udin|wawan|yoga|zaki|putra|permata|nurul|indah|ratu|cipta|bagus|wicaksono|prasetyo)\b", RegexOptions.IgnoreCase);
        private static readonly Regex TurkishPattern = new Regex(@"\b(mehmet|mustafa|ahmet|ali|huseyin|hasan|ibrahim|ismail|yusuf|osman|murat|emre|fatma|ayse|elif|zeynep|selin|deniz|cem|kaan|baris|onur|serkan|tolga|volkan|yilmaz|kaya|demir|celik|yildiz|aydin|ozturk|arslan|dogan|kilic)\b", RegexOptions.IgnoreCase);
        private static readonly Regex PolishPattern = new Regex(@"\b(jan|andrzej|piotr|krzysztof|tomasz|marek|michal|pawel|marcin|grzegorz|jakub|mateusz|katarzyna|anna|malgorzata|agnieszka|magdalena|joanna|barbara|ewal|kowalski|nowak|wisniewski|wójcik|kaminski|lewandowski|szymanski|wojciech|zielinski)\b", RegexOptions.IgnoreCase);
        private static readonly Regex RussianNamePattern = new Regex(@"\b(alexander|dmitry|sergei|ivan|andrei|nikolai|vladimir|mikhail|artem|maxim|anastasia|elena|olga|natalia|tatiana|ekaterina|maria|svetlana|irina|yulia|ivanov|petrov|sidorov|smirnov|kuznetsov|popov)\b", RegexOptions.IgnoreCase);
        private static readonly Regex ArabicNamePattern = new Regex(@"\b(mohammed|ahmed|ali|hassan|hussein|omar|khalid|faisal|abdullah|ibrahim|fatima|aisha|khadija|maryam|al\s|bin\s|abu\s|el\s)\b", RegexOptions.IgnoreCase);

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

            string lowerName = nickname.ToLowerInvariant();

            // === Phase 1: Keyword-based detection (highest priority) ===
            // Score all countries by keyword matches, pick the best
            var keywordScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var kvp in CountryKeywords)
            {
                string country = kvp.Key;
                foreach (string kw in kvp.Value)
                {
                    if (lowerName.Contains(kw.ToLowerInvariant()))
                    {
                        // Longer/more specific keywords get higher scores
                        float score = 85f + Math.Min(kw.Length * 1.5f, 10f);
                        if (!keywordScores.ContainsKey(country) || keywordScores[country] < score)
                            keywordScores[country] = score;
                    }
                }
            }

            if (keywordScores.Count > 0)
            {
                // Get the best matching country
                var best = keywordScores.OrderByDescending(x => x.Value).First();
                string bestCountry = best.Key;
                float bestScore = Math.Min(best.Value, 97f);

                result.PrimaryRegion = RegionAnalyzer.CountryCodeToRegion(bestCountry);
                result.Confidence = bestScore;
                result.CountryCode = bestCountry;
                result.AnalysisMethod = $"Keyword: {bestCountry}";

                // Build probability distribution
                result.Probabilities[result.PrimaryRegion] = bestScore;
                float remaining = 100f - bestScore;
                
                // Add secondary possibilities
                if (keywordScores.Count > 1)
                {
                    var second = keywordScores.OrderByDescending(x => x.Value).Skip(1).First();
                    string secondRegion = RegionAnalyzer.CountryCodeToRegion(second.Key);
                    if (secondRegion != result.PrimaryRegion)
                    {
                        float secondShare = remaining * 0.6f;
                        result.Probabilities[secondRegion] = secondShare;
                        remaining -= secondShare;
                    }
                }
                if (remaining > 2f)
                    result.Probabilities["Other"] = remaining;

                return result;
            }

            // === Phase 2: Unicode script detection ===
            bool hasCJK = CJKUnified.IsMatch(nickname);
            bool hasHiragana = Hiragana.IsMatch(nickname);
            bool hasKatakana = Katakana.IsMatch(nickname);
            bool hasHangul = HangulSyllables.IsMatch(nickname) || HangulJamo.IsMatch(nickname);
            bool hasCyrillic = Cyrillic.IsMatch(nickname);
            bool hasArabic = Arabic.IsMatch(nickname);
            bool hasThai = Thai.IsMatch(nickname);
            bool hasLao = Lao.IsMatch(nickname);
            bool hasKhmer = Khmer.IsMatch(nickname);
            bool hasMyanmar = Myanmar.IsMatch(nickname);
            bool hasTibetan = Tibetan.IsMatch(nickname);
            bool hasDevanagari = Devanagari.IsMatch(nickname);
            bool hasBengali = Bengali.IsMatch(nickname);
            bool hasGreek = Greek.IsMatch(nickname);
            bool hasHebrew = Hebrew.IsMatch(nickname);
            bool hasArmenian = Armenian.IsMatch(nickname);
            bool hasGeorgian = Georgian.IsMatch(nickname);
            bool hasEthiopic = Ethiopic.IsMatch(nickname);
            bool hasSinhala = Sinhala.IsMatch(nickname);
            bool hasTamil = Tamil.IsMatch(nickname);
            bool hasTelugu = Telugu.IsMatch(nickname);
            bool hasLatin = Latin.IsMatch(nickname);

            // Japanese: Hiragana or Katakana is definitive
            if (hasHiragana || hasKatakana)
            {
                result.Probabilities["Japan"] = 94f;
                result.Probabilities["Other"] = 6f;
                result.PrimaryRegion = "Japan";
                result.Confidence = 94f;
                result.AnalysisMethod = "Kana detected";
                result.CountryCode = "JP";
                return result;
            }

            // Korean: Hangul is definitive
            if (hasHangul)
            {
                result.Probabilities["Korea"] = 94f;
                result.Probabilities["Other"] = 6f;
                result.PrimaryRegion = "Korea";
                result.Confidence = 94f;
                result.AnalysisMethod = "Hangul detected";
                result.CountryCode = "KR";
                return result;
            }

            // CJK without kana/hangul -> likely Chinese
            if (hasCJK && !hasHiragana && !hasKatakana && !hasHangul)
            {
                result.Probabilities["China"] = 78f;
                result.Probabilities["Japan"] = 7f;
                result.Probabilities["Korea"] = 6f;
                result.Probabilities["Vietnam"] = 4f;
                result.Probabilities["Other"] = 5f;
                result.PrimaryRegion = "China";
                result.Confidence = 78f;
                result.AnalysisMethod = "CJK Hanzi detected";
                result.CountryCode = "CN";
                return result;
            }

            // Thai -> Thailand
            if (hasThai)
            {
                result.Probabilities["Thailand"] = 90f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Thailand";
                result.Confidence = 90f;
                result.AnalysisMethod = "Thai script detected";
                result.CountryCode = "TH";
                return result;
            }

            // Lao -> Laos
            if (hasLao)
            {
                result.Probabilities["Laos"] = 85f;
                result.Probabilities["Thailand"] = 10f;
                result.Probabilities["Other"] = 5f;
                result.PrimaryRegion = "Laos";
                result.Confidence = 85f;
                result.AnalysisMethod = "Lao script detected";
                result.CountryCode = "LA";
                return result;
            }

            // Khmer -> Cambodia
            if (hasKhmer)
            {
                result.Probabilities["Cambodia"] = 88f;
                result.Probabilities["Other"] = 12f;
                result.PrimaryRegion = "Cambodia";
                result.Confidence = 88f;
                result.AnalysisMethod = "Khmer script detected";
                result.CountryCode = "KH";
                return result;
            }

            // Myanmar -> Myanmar
            if (hasMyanmar)
            {
                result.Probabilities["Myanmar"] = 88f;
                result.Probabilities["Other"] = 12f;
                result.PrimaryRegion = "Myanmar";
                result.Confidence = 88f;
                result.AnalysisMethod = "Myanmar script detected";
                result.CountryCode = "MM";
                return result;
            }

            // Tibetan -> Tibet / China
            if (hasTibetan)
            {
                result.Probabilities["China"] = 60f;
                result.Probabilities["Other"] = 40f;
                result.PrimaryRegion = "China";
                result.Confidence = 60f;
                result.AnalysisMethod = "Tibetan script detected";
                result.CountryCode = "CN";
                return result;
            }

            // Devanagari -> India / Nepal
            if (hasDevanagari)
            {
                result.Probabilities["India"] = 80f;
                result.Probabilities["Nepal"] = 12f;
                result.Probabilities["Other"] = 8f;
                result.PrimaryRegion = "India";
                result.Confidence = 80f;
                result.AnalysisMethod = "Devanagari detected";
                result.CountryCode = "IN";
                return result;
            }

            // Bengali -> Bangladesh / India (West Bengal)
            if (hasBengali)
            {
                result.Probabilities["Bangladesh"] = 45f;
                result.Probabilities["India"] = 45f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Bangladesh";
                result.Confidence = 45f;
                result.AnalysisMethod = "Bengali detected";
                result.CountryCode = "BD";
                return result;
            }

            // Tamil -> India (Tamil Nadu) / Sri Lanka
            if (hasTamil)
            {
                result.Probabilities["India"] = 60f;
                result.Probabilities["Sri Lanka"] = 25f;
                result.Probabilities["Other"] = 15f;
                result.PrimaryRegion = "India";
                result.Confidence = 60f;
                result.AnalysisMethod = "Tamil detected";
                result.CountryCode = "IN";
                return result;
            }

            // Telugu -> India (Andhra/Telangana)
            if (hasTelugu)
            {
                result.Probabilities["India"] = 85f;
                result.Probabilities["Other"] = 15f;
                result.PrimaryRegion = "India";
                result.Confidence = 85f;
                result.AnalysisMethod = "Telugu detected";
                result.CountryCode = "IN";
                return result;
            }

            // Sinhala -> Sri Lanka
            if (hasSinhala)
            {
                result.Probabilities["Sri Lanka"] = 88f;
                result.Probabilities["Other"] = 12f;
                result.PrimaryRegion = "Sri Lanka";
                result.Confidence = 88f;
                result.AnalysisMethod = "Sinhala detected";
                result.CountryCode = "LK";
                return result;
            }

            // Ethiopic -> Ethiopia / Eritrea
            if (hasEthiopic)
            {
                result.Probabilities["Ethiopia"] = 70f;
                result.Probabilities["Eritrea"] = 20f;
                result.Probabilities["Other"] = 10f;
                result.PrimaryRegion = "Ethiopia";
                result.Confidence = 70f;
                result.AnalysisMethod = "Ethiopic detected";
                result.CountryCode = "ET";
                return result;
            }

            // Cyrillic -> Russia/CIS (use name patterns to differentiate)
            if (hasCyrillic)
            {
                bool isRussianName = RussianNamePattern.IsMatch(nickname);
                bool isUkrainian = lowerName.Contains("\u0443\u043a\u0440") || lowerName.Contains("ua ") || lowerName.Contains("[ua]");
                bool isBelarusian = lowerName.Contains("\u0431\u0435\u043b\u0430\u0440\u0443\u0441") || lowerName.Contains("by ") || lowerName.Contains("[by]");

                if (isUkrainian)
                {
                    result.Probabilities["Ukraine"] = 75f;
                    result.Probabilities["Russia"] = 10f;
                    result.Probabilities["Other"] = 15f;
                    result.PrimaryRegion = "Ukraine";
                    result.Confidence = 75f;
                    result.AnalysisMethod = "Cyrillic + UA keywords";
                    result.CountryCode = "UA";
                }
                else if (isBelarusian)
                {
                    result.Probabilities["Belarus"] = 75f;
                    result.Probabilities["Russia"] = 10f;
                    result.Probabilities["Other"] = 15f;
                    result.PrimaryRegion = "Belarus";
                    result.Confidence = 75f;
                    result.AnalysisMethod = "Cyrillic + BY keywords";
                    result.CountryCode = "BY";
                }
                else
                {
                    result.Probabilities["Russia"] = 55f;
                    result.Probabilities["Ukraine"] = 12f;
                    result.Probabilities["Belarus"] = 8f;
                    result.Probabilities["Other CIS"] = 10f;
                    result.Probabilities["Balkans"] = 5f;
                    result.Probabilities["Other"] = 10f;
                    result.PrimaryRegion = "Russia";
                    result.Confidence = 55f;
                    result.AnalysisMethod = "Cyrillic detected";
                    result.CountryCode = "RU";
                }
                return result;
            }

            // Arabic -> Middle East / North Africa
            if (hasArabic)
            {
                bool isArabicName = ArabicNamePattern.IsMatch(nickname);
                bool isEgyptian = lowerName.Contains("egypt") || lowerName.Contains("\u0645\u0635\u0631");
                
                if (isEgyptian)
                {
                    result.Probabilities["Egypt"] = 65f;
                    result.Probabilities["Middle East"] = 15f;
                    result.Probabilities["Other"] = 20f;
                    result.PrimaryRegion = "Egypt";
                    result.Confidence = 65f;
                    result.AnalysisMethod = "Arabic + EG keywords";
                    result.CountryCode = "EG";
                }
                else
                {
                    result.Probabilities["Middle East"] = 50f;
                    result.Probabilities["North Africa"] = 20f;
                    result.Probabilities["Central Asia"] = 10f;
                    result.Probabilities["Other"] = 20f;
                    result.PrimaryRegion = "Middle East";
                    result.Confidence = 50f;
                    result.AnalysisMethod = "Arabic script detected";
                    result.CountryCode = "SA";
                }
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

            // === Phase 3: Latin script analysis (lower confidence) ===
            if (hasLatin)
            {
                // Check for specific Unicode character patterns (high reliability)
                bool hasVietnameseDiacritics = Regex.IsMatch(nickname, @"[\u0103\u00E2\u00EA\u00F4\u01A1\u01B0\u0111\u00E0\u00E1\u00E3\u00E8\u00E9\u00EC\u00ED\u00F2\u00F3\u00F9\u00FA\u00FD\u1EA3\u1EA1\u1EB9\u1EC7\u1ECF\u1ED3\u1EDD\u1EE5\u1EEF]");
                bool hasTurkishChars = Regex.IsMatch(nickname, @"[\u00E7\u011F\u0131\u00F6\u015F\u00FC]", RegexOptions.IgnoreCase);
                bool hasPolishChars = Regex.IsMatch(nickname, @"[\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C]", RegexOptions.IgnoreCase);
                bool hasScandinavianChars = Regex.IsMatch(nickname, @"[\u00E4\u00E5\u00F6\u00E6\u00F8]", RegexOptions.IgnoreCase);
                bool hasCzechChars = Regex.IsMatch(nickname, @"[\u010D\u011B\u0148\u0159\u0161\u0165\u016F\u017E]", RegexOptions.IgnoreCase);
                bool hasRomanianChars = Regex.IsMatch(nickname, @"[\u0103\u00E2\u00EE\u0219\u021B]", RegexOptions.IgnoreCase);
                bool hasHungarianChars = Regex.IsMatch(nickname, @"[\u00E1\u00E9\u00ED\u00F3\u00F6\u0151\u00FA\u00FC\u0171]", RegexOptions.IgnoreCase);

                // Check name patterns
                bool isSpanish = SpanishPattern.IsMatch(nickname);
                bool isGerman = GermanPattern.IsMatch(nickname);
                bool isFrench = FrenchPattern.IsMatch(nickname);
                bool isVietnamese = VietnamesePattern.IsMatch(nickname);
                bool isIndonesian = IndonesianPattern.IsMatch(nickname);
                bool isTurkishName = TurkishPattern.IsMatch(nickname);
                bool isPolishName = PolishPattern.IsMatch(nickname);
                bool isRussianLatin = RussianNamePattern.IsMatch(nickname);
                bool isArabicLatin = ArabicNamePattern.IsMatch(nickname);

                // Vietnamese (diacritics are very distinctive)
                if (isVietnamese || hasVietnameseDiacritics)
                {
                    result.Probabilities["Vietnam"] = 78f;
                    result.Probabilities["Other"] = 22f;
                    result.PrimaryRegion = "Vietnam";
                    result.Confidence = 78f;
                    result.AnalysisMethod = "Vietnamese pattern/diacritics";
                    result.CountryCode = "VN";
                    return result;
                }

                // Turkish
                if (hasTurkishChars || isTurkishName)
                {
                    float conf = (hasTurkishChars && isTurkishName) ? 80f : 72f;
                    result.Probabilities["Turkey"] = conf;
                    result.Probabilities["Other"] = 100f - conf;
                    result.PrimaryRegion = "Turkey";
                    result.Confidence = conf;
                    result.AnalysisMethod = "Turkish pattern/chars";
                    result.CountryCode = "TR";
                    return result;
                }

                // Polish
                if (hasPolishChars || isPolishName)
                {
                    float conf = (hasPolishChars && isPolishName) ? 78f : 65f;
                    result.Probabilities["Poland"] = conf;
                    result.Probabilities["Czech"] = 5f;
                    result.Probabilities["Other"] = 100f - conf - 5f;
                    result.PrimaryRegion = "Poland";
                    result.Confidence = conf;
                    result.AnalysisMethod = "Polish pattern/chars";
                    result.CountryCode = "PL";
                    return result;
                }

                // Czech
                if (hasCzechChars)
                {
                    result.Probabilities["Czech"] = 70f;
                    result.Probabilities["Slovakia"] = 12f;
                    result.Probabilities["Other"] = 18f;
                    result.PrimaryRegion = "Czech";
                    result.Confidence = 70f;
                    result.AnalysisMethod = "Czech characters";
                    result.CountryCode = "CZ";
                    return result;
                }

                // Romanian
                if (hasRomanianChars)
                {
                    result.Probabilities["Romania"] = 70f;
                    result.Probabilities["Moldova"] = 8f;
                    result.Probabilities["Other"] = 22f;
                    result.PrimaryRegion = "Romania";
                    result.Confidence = 70f;
                    result.AnalysisMethod = "Romanian characters";
                    result.CountryCode = "RO";
                    return result;
                }

                // Hungarian
                if (hasHungarianChars && (isGerman == false))
                {
                    // Hungarian shares some chars with German, but unique combo
                    result.Probabilities["Hungary"] = 60f;
                    result.Probabilities["Germany"] = 10f;
                    result.Probabilities["Other"] = 30f;
                    result.PrimaryRegion = "Hungary";
                    result.Confidence = 60f;
                    result.AnalysisMethod = "Hungarian characters";
                    result.CountryCode = "HU";
                    return result;
                }

                // Scandinavian
                if (hasScandinavianChars)
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

                // Indonesian/Malay
                if (isIndonesian)
                {
                    result.Probabilities["Indonesia"] = 65f;
                    result.Probabilities["Malaysia"] = 10f;
                    result.Probabilities["Other"] = 25f;
                    result.PrimaryRegion = "Indonesia";
                    result.Confidence = 65f;
                    result.AnalysisMethod = "Indonesian name pattern";
                    result.CountryCode = "ID";
                    return result;
                }

                // Arabic name in Latin script
                if (isArabicLatin)
                {
                    result.Probabilities["Middle East"] = 50f;
                    result.Probabilities["North Africa"] = 20f;
                    result.Probabilities["Other"] = 30f;
                    result.PrimaryRegion = "Middle East";
                    result.Confidence = 50f;
                    result.AnalysisMethod = "Arabic name (Latin)";
                    result.CountryCode = "SA";
                    return result;
                }

                // Russian name in Latin script
                if (isRussianLatin)
                {
                    result.Probabilities["Russia"] = 45f;
                    result.Probabilities["Other CIS"] = 15f;
                    result.Probabilities["Other"] = 40f;
                    result.PrimaryRegion = "Russia";
                    result.Confidence = 45f;
                    result.AnalysisMethod = "Russian name (Latin)";
                    result.CountryCode = "RU";
                    return result;
                }

                // Spanish pattern
                if (isSpanish)
                {
                    result.Probabilities["Latin America"] = 35f;
                    result.Probabilities["Spain"] = 25f;
                    result.Probabilities["Other"] = 40f;
                    result.PrimaryRegion = "Latin America";
                    result.Confidence = 35f;
                    result.AnalysisMethod = "Spanish pattern";
                    result.CountryCode = "MX";
                    return result;
                }

                // German pattern
                if (isGerman)
                {
                    result.Probabilities["Germany"] = 45f;
                    result.Probabilities["Austria"] = 12f;
                    result.Probabilities["Nordic"] = 8f;
                    result.Probabilities["Other"] = 35f;
                    result.PrimaryRegion = "Germany";
                    result.Confidence = 45f;
                    result.AnalysisMethod = "German pattern";
                    result.CountryCode = "DE";
                    return result;
                }

                // French pattern
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

                // Portuguese characters (Brazil vs Portugal)
                bool hasPortuguese = Regex.IsMatch(nickname, @"[\u00E3\u00F5\u00E7\u00E1\u00E9\u00ED\u00F3\u00FA]", RegexOptions.IgnoreCase);
                if (hasPortuguese)
                {
                    // Check for Brazil-specific hints
                    if (lowerName.Contains("br") || lowerName.Contains("brasil"))
                    {
                        result.Probabilities["Brazil"] = 70f;
                        result.Probabilities["Portugal"] = 10f;
                        result.Probabilities["Other"] = 20f;
                        result.PrimaryRegion = "Brazil";
                        result.Confidence = 70f;
                        result.AnalysisMethod = "Portuguese + BR hint";
                        result.CountryCode = "BR";
                    }
                    else
                    {
                        result.Probabilities["Brazil"] = 45f;
                        result.Probabilities["Portugal"] = 20f;
                        result.Probabilities["Other"] = 35f;
                        result.PrimaryRegion = "Brazil";
                        result.Confidence = 45f;
                        result.AnalysisMethod = "Portuguese characters";
                        result.CountryCode = "BR";
                    }
                    return result;
                }

                // Generic Latin -> broad distribution
                result.Probabilities["Western Europe"] = 18f;
                result.Probabilities["North America"] = 18f;
                result.Probabilities["Latin America"] = 12f;
                result.Probabilities["Southeast Asia"] = 10f;
                result.Probabilities["Eastern Europe"] = 8f;
                result.Probabilities["Other"] = 34f;
                result.PrimaryRegion = "Western Europe";
                result.Confidence = 18f;
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
