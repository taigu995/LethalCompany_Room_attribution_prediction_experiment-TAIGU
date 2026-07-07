using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LethalCompanyRegionTag.UI
{
    /// <summary>
    /// Manages CJK font loading and fallback for region tag display.
    /// Scans for existing CJK fonts from localization mods or user-provided fonts.
    /// </summary>
    public static class FontManager
    {
        private static bool _initialized = false;
        private static bool _cjkFontAvailable = false;
        private static TMP_FontAsset _cjkFontAsset = null;
        private static readonly List<TMP_FontAsset> _loadedFonts = new List<TMP_FontAsset>();

        // Known CJK font file names from popular Chinese localization mods
        private static readonly string[] KnownCjkFontFiles = new string[]
        {
            "arialuni_sdf_u2019",
            "Sinter-Normal",
            "sinter-normal",
            "NotoSansSC",
            "NotoSansCJK",
            "SourceHanSans",
            "WenQuanYi",
            "MicrosoftYaHei",
            "msyh",
            "simhei",
            "simsun",
            "arialunicode",
            "arialuni",
            "droidsansfallback",
            "cjk"
        };

        // File extensions to search
        private static readonly string[] FontExtensions = new string[]
        {
            ".assets",
            ".unity3d",
            ".bundle",
            ".fontsettings"
        };

        /// <summary>
        /// Whether CJK font is available for rendering Chinese characters.
        /// </summary>
        public static bool CjkFontAvailable
        {
            get { return _cjkFontAvailable; }
        }

        /// <summary>
        /// The loaded CJK font asset, or null if not available.
        /// </summary>
        public static TMP_FontAsset CjkFontAsset
        {
            get { return _cjkFontAsset; }
        }

        /// <summary>
        /// Initialize font manager - scan and load CJK fonts.
        /// Should be called once during plugin startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Plugin.LogSource.LogInfo("[TAIGU] FontManager: Scanning for CJK fonts...");

            // Strategy 1: Search for already loaded TMP fonts in the project
            TryFindLoadedCjkFonts();

            // Strategy 2: Search for font asset files in game directory
            if (!_cjkFontAvailable)
            {
                TryLoadFontsFromDirectory(Application.dataPath);
            }

            // Strategy 3: Search in BepInEx plugin directory
            if (!_cjkFontAvailable)
            {
                string bepinexPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx");
                TryLoadFontsFromDirectory(bepinexPath);
            }

            // Strategy 4: Search in game root directory
            if (!_cjkFontAvailable)
            {
                string gameRoot = Path.GetDirectoryName(Application.dataPath);
                TryLoadFontsFromDirectory(gameRoot);
            }

            // Strategy 5: Try to find any TMP font with CJK characters via Resources
            if (!_cjkFontAvailable)
            {
                TryLoadFromResources();
            }

            if (_cjkFontAvailable)
            {
                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: CJK font loaded successfully - '{_cjkFontAsset.name}'");

                // Add as fallback to the game's default font
                TryAddFallbackToGameFont();
            }
            else
            {
                Plugin.LogSource.LogWarning("[TAIGU] FontManager: No CJK font found. Chinese characters will display as ASCII codes.");
                Plugin.LogSource.LogWarning("[TAIGU] FontManager: Install a Chinese localization mod or place a CJK font file in the game directory.");
            }
        }

        /// <summary>
        /// Try to find CJK fonts that are already loaded in the project.
        /// </summary>
        private static void TryFindLoadedCjkFonts()
        {
            try
            {
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var font in allFonts)
                {
                    if (font == null || font.name == null) continue;
                    if (HasCjkSupport(font))
                    {
                        SetCjkFont(font);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error scanning loaded fonts: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to load font assets from a directory.
        /// </summary>
        private static void TryLoadFontsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory)) return;

            try
            {
                // Search for known CJK font files
                foreach (string fontName in KnownCjkFontFiles)
                {
                    foreach (string ext in FontExtensions)
                    {
                        string pattern = $"*{fontName}*{ext}";
                        string[] files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            if (TryLoadFontFromFile(file))
                                return;
                        }
                    }
                }

                // Also search for any .assets files that might contain fonts
                string[] assetFiles = Directory.GetFiles(directory, "*.assets", SearchOption.TopDirectoryOnly);
                foreach (string file in assetFiles)
                {
                    if (TryLoadFontFromFile(file))
                        return;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error scanning directory {directory}: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to load a TMP font asset from a file.
        /// </summary>
        private static bool TryLoadFontFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                // Try loading as AssetBundle
                var bundleLoadRequest = AssetBundle.LoadFromFileAsync(filePath);
                // Wait for completion (synchronous in practice for local files)
                while (!bundleLoadRequest.isDone)
                {
                    System.Threading.Thread.Sleep(10);
                }

                AssetBundle bundle = bundleLoadRequest.assetBundle;
                if (bundle == null) return false;

                TMP_FontAsset[] fonts = bundle.LoadAllAssets<TMP_FontAsset>();
                if (fonts != null && fonts.Length > 0)
                {
                    foreach (var font in fonts)
                    {
                        if (font != null)
                        {
                            _loadedFonts.Add(font);
                            if (HasCjkSupport(font))
                            {
                                SetCjkFont(font);
                                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Loaded CJK font from: {filePath}");
                                return true;
                            }
                        }
                    }
                }

                // Don't unload - keep the bundle loaded so fonts remain valid
                return false;
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Failed to load font from {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to find CJK fonts via Unity Resources.
        /// </summary>
        private static void TryLoadFromResources()
        {
            try
            {
                // Try common resource paths
                string[] resourcePaths = new string[]
                {
                    "Fonts/arialuni_sdf_u2019",
                    "Fonts/Sinter-Normal",
                    "Fonts/NotoSansSC",
                    "Fonts/cjk",
                    "arialuni_sdf_u2019",
                    "Sinter-Normal"
                };

                foreach (string path in resourcePaths)
                {
                    TMP_FontAsset font = Resources.Load<TMP_FontAsset>(path);
                    if (font != null && HasCjkSupport(font))
                    {
                        SetCjkFont(font);
                        Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Loaded CJK font from Resources: {path}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error loading from Resources: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a TMP font asset contains CJK characters.
        /// </summary>
        private static bool HasCjkSupport(TMP_FontAsset font)
        {
            if (font == null || font.characterLookupTable == null) return false;

            // Check for common CJK characters
            // Chinese: 中 (U+4E2D), 国 (U+56FD)
            // Japanese: あ (U+3042), ア (U+30A2)
            // Korean: 가 (U+AC00)
            char[] testChars = new char[] { '\u4E2D', '\u56FD', '\u3042', '\u30A2', '\uAC00' };

            int found = 0;
            foreach (char c in testChars)
            {
                if (font.characterLookupTable.ContainsKey(c))
                    found++;
            }

            // If at least 2 CJK characters are found, consider it a CJK font
            return found >= 2;
        }

        /// <summary>
        /// Set the CJK font asset and mark as available.
        /// </summary>
        private static void SetCjkFont(TMP_FontAsset font)
        {
            _cjkFontAsset = font;
            _cjkFontAvailable = true;
        }

        /// <summary>
        /// Try to add the CJK font as a fallback to the game's default font.
        /// </summary>
        private static void TryAddFallbackToGameFont()
        {
            if (_cjkFontAsset == null) return;

            try
            {
                // Try to find the game's default font
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var font in allFonts)
                {
                    if (font != null && font.name != null && font.name.Contains("3270"))
                    {
                        AddFallbackFont(font, _cjkFontAsset);
                        Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Added CJK fallback to game font '{font.name}'");
                        return;
                    }
                }

                // If we can't find the specific game font, try any font
                if (allFonts.Length > 0)
                {
                    foreach (var font in allFonts)
                    {
                        if (font != null && font != _cjkFontAsset)
                        {
                            AddFallbackFont(font, _cjkFontAsset);
                            Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Added CJK fallback to font '{font.name}'");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error adding fallback: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a fallback font to a target font if not already present.
        /// </summary>
        private static void AddFallbackFont(TMP_FontAsset target, TMP_FontAsset fallback)
        {
            if (target == null || fallback == null) return;

            // Check if fallback is already in the list
            var existingFallbacks = target.fallbackFontAssetTable;
            if (existingFallbacks == null)
            {
                target.fallbackFontAssetTable = new List<TMP_FontAsset>();
                existingFallbacks = target.fallbackFontAssetTable;
            }

            if (!existingFallbacks.Contains(fallback))
            {
                existingFallbacks.Add(fallback);
            }
        }

        /// <summary>
        /// Apply CJK font support to a specific TMP text component.
        /// Call this when creating new tag labels.
        /// </summary>
        public static void ApplyFontSupport(TextMeshProUGUI textComponent)
        {
            if (textComponent == null) return;

            if (_cjkFontAvailable && _cjkFontAsset != null)
            {
                // Add CJK font as fallback to this text component's font
                AddFallbackFont(textComponent.font, _cjkFontAsset);
            }
        }

        /// <summary>
        /// Get the display label for a region code.
        /// Returns Chinese name if CJK font is available, otherwise returns ASCII code.
        /// </summary>
        public static string GetDisplayLabel(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode)) return "??";

            // If CJK font is available, use Chinese names
            if (_cjkFontAvailable)
            {
                return RegionCodeToChinese(regionCode);
            }

            // Otherwise, return the ASCII code
            return regionCode;
        }

        /// <summary>
        /// Map region code to Chinese name.
        /// </summary>
        private static string RegionCodeToChinese(string code)
        {
            switch (code.ToUpperInvariant())
            {
                // East Asia
                case "CN": return "\u4e2d\u56fd";       // 中国
                case "TW": return "\u53f0\u6e7e";       // 台湾
                case "HK": return "\u9999\u6e2f";       // 香港
                case "JP": return "\u65e5\u672c";       // 日本
                case "KR": return "\u97e9\u56fd";       // 韩国

                // Southeast Asia
                case "VN": return "\u8d8a\u5357";       // 越南
                case "TH": return "\u6cf0\u56fd";       // 泰国
                case "ID": return "\u5370\u5c3c";       // 印尼
                case "MY": return "\u9a6c\u6765";       // 马来
                case "PH": return "\u83f2\u5f8b\u5bbe"; // 菲律宾
                case "SG": return "\u65b0\u52a0\u5761"; // 新加坡
                case "KH": return "\u67ec\u57d4\u5be8"; // 柬埔寨
                case "LA": return "\u8001\u631d";       // 老挝
                case "MM": return "\u7f05\u7538";       // 缅甸

                // South Asia
                case "IN": return "\u5370\u5ea6";       // 印度
                case "PK": return "\u5df4\u57fa\u65af\u5766"; // 巴基斯坦
                case "BD": return "\u5b5f\u52a0\u62c9"; // 孟加拉

                // Central Asia
                case "KZ": return "\u54c8\u8428\u514b"; // 哈萨克
                case "UZ": return "\u4e4c\u5179\u522b\u514b"; // 乌兹别克
                case "MN": return "\u8499\u53e4";       // 蒙古

                // Europe
                case "RU": return "\u4fc4\u7f57\u65af"; // 俄罗斯
                case "PL": return "\u6ce2\u5170";       // 波兰
                case "CZ": return "\u6377\u514b";       // 捷克
                case "SK": return "\u65af\u6d1b\u4f10\u514b"; // 斯洛伐克
                case "HU": return "\u5308\u7259\u5229"; // 匈牙利
                case "RO": return "\u7f57\u9a6c\u5c3c\u4e9a"; // 罗马尼亚
                case "BG": return "\u4fdd\u52a0\u5229\u4e9a"; // 保加利亚
                case "UA": return "\u4e4c\u514b\u5170"; // 乌克兰
                case "BY": return "\u767d\u4fc4\u7f57\u65af"; // 白俄罗斯
                case "TR": return "\u571f\u8033\u5176"; // 土耳其
                case "GR": return "\u5e0c\u814a";       // 希腊
                case "IT": return "\u610f\u5927\u5229"; // 意大利
                case "ES": return "\u897f\u73ed\u7259"; // 西班牙
                case "PT": return "\u8461\u8404\u7259"; // 葡萄牙
                case "FR": return "\u6cd5\u56fd";       // 法国
                case "DE": return "\u5fb7\u56fd";       // 德国
                case "NL": return "\u8377\u5170";       // 荷兰
                case "BE": return "\u6bd4\u5229\u65f6"; // 比利时
                case "GB": return "\u82f1\u56fd";       // 英国
                case "UK": return "\u82f1\u56fd";       // 英国
                case "SE": return "\u745e\u5178";       // 瑞典
                case "NO": return "\u632a\u5a01";       // 挪威
                case "DK": return "\u4e39\u9ea6";       // 丹麦
                case "FI": return "\u82ac\u5170";       // 芬兰
                case "IE": return "\u7231\u5c14\u5170"; // 爱尔兰
                case "AT": return "\u5965\u5730\u5229"; // 奥地利
                case "CH": return "\u745e\u58eb";       // 瑞士
                case "HR": return "\u514b\u7f57\u5730\u4e9a"; // 克罗地亚
                case "RS": return "\u585e\u5c14\u7ef4\u4e9a"; // 塞尔维亚
                case "LT": return "\u7acb\u9676\u5b9b"; // 立陶宛
                case "LV": return "\u62c9\u8131\u7ef4\u4e9a"; // 拉脱维亚
                case "EE": return "\u7231\u6c99\u5c3c\u4e9a"; // 爱沙尼亚

                // Americas
                case "US": return "\u7f8e\u56fd";       // 美国
                case "CA": return "\u52a0\u62ff\u5927"; // 加拿大
                case "MX": return "\u58a8\u897f\u54e5"; // 墨西哥
                case "BR": return "\u5df4\u897f";       // 巴西
                case "AR": return "\u963f\u6839\u5ef7"; // 阿根廷
                case "CL": return "\u667a\u5229";       // 智利
                case "CO": return "\u54e5\u4f26\u6bd4\u4e9a"; // 哥伦比亚
                case "PE": return "\u79d8\u9c81";       // 秘鲁

                // Middle East
                case "SA": return "\u6c99\u7279";       // 沙特
                case "AE": return "\u8fea\u62dc";       // 迪拜
                case "IR": return "\u4f0a\u6717";       // 伊朗
                case "IQ": return "\u4f0a\u62c9\u514b"; // 伊拉克
                case "IL": return "\u4ee5\u8272\u5217"; // 以色列
                case "EG": return "\u57c3\u53ca";       // 埃及

                // Africa
                case "ZA": return "\u5357\u975e";       // 南非
                case "NG": return "\u5c3c\u65e5\u5229\u4e9a"; // 尼日利亚
                case "KE": return "\u80af\u5c3c\u4e9a"; // 肯尼亚
                case "MA": return "\u6469\u6d1b\u54e5"; // 摩洛哥
                case "DZ": return "\u963f\u5c14\u53ca\u5229\u4e9a"; // 阿尔及利亚
                case "ET": return "\u57c3\u585e\u4fc4\u6bd4\u4e9a"; // 埃塞俄比亚

                // Oceania
                case "AU": return "\u6fb3\u5927\u5229\u4e9a"; // 澳大利亚
                case "NZ": return "\u65b0\u897f\u5170"; // 新西兰

                // Aggregated regions
                case "WEST": return "\u6b27\u7f8e";     // 欧美
                case "EAST": return "\u4e1c\u6b27";     // 东欧
                case "NORD": return "\u5317\u6b27";     // 北欧
                case "SEA": return "\u4e1c\u5357\u4e9a"; // 东南亚
                case "SASIA": return "\u5357\u4e9a";    // 南亚
                case "CASIA": return "\u4e2d\u4e9a";    // 中亚
                case "MENA": return "\u4e2d\u4e1c";     // 中东
                case "LATAM": return "\u62c9\u7f8e";    // 拉美
                case "AFR": return "\u975e\u6d32";      // 非洲
                case "OCE": return "\u5927\u6d0b\u6d32"; // 大洋洲

                default: return code;
            }
        }
    }
}
