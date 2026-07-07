using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LethalCompanyRegionTag.UI
{
    /// <summary>
    /// Manages CJK font loading and fallback for region tag display.
    /// Supports loading TTF files directly from the plugin directory.
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

        // File extensions to search for TMP font assets
        private static readonly string[] FontExtensions = new string[]
        {
            ".assets",
            ".unity3d",
            ".bundle",
            ".fontsettings"
        };

        // TTF file extensions
        private static readonly string[] TtfExtensions = new string[]
        {
            ".ttf",
            ".otf"
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

            // Strategy 0 (Priority): Load TTF from plugin directory
            TryLoadTtfFromPluginDirectory();

            // Strategy 1: Search for already loaded TMP fonts in the project
            if (!_cjkFontAvailable)
            {
                TryFindLoadedCjkFonts();
            }

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
                Plugin.LogSource.LogWarning("[TAIGU] FontManager: Install a Chinese localization mod or place a CJK font file (TTF/OTF) in BepInEx/plugins/LethalCompanyRegionTag/ directory.");
            }
        }

        /// <summary>
        /// Strategy 0: Try to load TTF/OTF font files directly from the plugin directory.
        /// This is the primary strategy for loading user-provided fonts like Microsoft YaHei.
        /// </summary>
        private static void TryLoadTtfFromPluginDirectory()
        {
            try
            {
                string pluginDir = Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    "BepInEx", "plugins", "LethalCompanyRegionTag"
                );

                if (!Directory.Exists(pluginDir))
                {
                    Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Plugin directory not found: {pluginDir}");
                    return;
                }

                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Scanning plugin directory for TTF fonts: {pluginDir}");

                // Search for TTF/OTF files
                foreach (string ext in TtfExtensions)
                {
                    string[] files = Directory.GetFiles(pluginDir, $"*{ext}", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        if (TryLoadTtfFont(file))
                        {
                            Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Successfully loaded TTF font from: {file}");
                            return;
                        }
                    }
                }

                // Also check for known CJK font names specifically
                foreach (string fontName in KnownCjkFontFiles)
                {
                    foreach (string ext in TtfExtensions)
                    {
                        string filePath = Path.Combine(pluginDir, fontName + ext);
                        if (File.Exists(filePath))
                        {
                            if (TryLoadTtfFont(filePath))
                            {
                                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Successfully loaded known CJK font: {filePath}");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error loading TTF from plugin directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to load a TTF/OTF font file and create a TMP FontAsset from it.
        /// Uses multiple strategies to maximize compatibility across Unity versions.
        /// </summary>
        private static bool TryLoadTtfFont(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Attempting to load TTF font: {filePath}");

                string fileName = Path.GetFileNameWithoutExtension(filePath);
                Font unityFont = null;

                // Strategy 1: Direct path loading (works in some Unity versions)
                try
                {
                    unityFont = new Font(filePath);
                    if (unityFont != null && unityFont.name != null)
                    {
                        Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Loaded font via direct path: {unityFont.name}");
                    }
                    else
                    {
                        unityFont = null;
                    }
                }
                catch { unityFont = null; }

                // Strategy 2: Load by font family name from file name
                if (unityFont == null)
                {
                    try
                    {
                        unityFont = Font.CreateDynamicFontFromOSFont(fileName, 16);
                        if (unityFont != null)
                        {
                            Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Loaded font via OS font name: {fileName}");
                        }
                    }
                    catch { unityFont = null; }
                }

                // Strategy 3: Try known font family names for common CJK fonts
                if (unityFont == null)
                {
                    string[] knownNames = new string[]
                    {
                        "Microsoft YaHei", "Microsoft YaHei UI",
                        "SimHei", "SimSun", "NSimSun", "KaiTi",
                        "Noto Sans SC", "Noto Sans CJK SC", "Noto Sans",
                        "Source Han Sans SC", "Source Han Sans CN",
                        "WenQuanYi Micro Hei", "WenQuanYi Zen Hei",
                        "PingFang SC", "Hiragino Sans GB",
                        fileName, fileName.Replace("_", " "), fileName.Replace("-", " ")
                    };

                    foreach (string fontName in knownNames)
                    {
                        if (string.IsNullOrEmpty(fontName)) continue;
                        try
                        {
                            unityFont = Font.CreateDynamicFontFromOSFont(fontName, 16);
                            if (unityFont != null)
                            {
                                Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Loaded font via known name: {fontName}");
                                break;
                            }
                        }
                        catch { unityFont = null; }
                    }
                }

                if (unityFont == null)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Could not load font from: {filePath}");
                    return false;
                }

                // Create a TMP FontAsset from the Unity Font
                // Using dynamic SDF mode for runtime font generation
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(unityFont);
                if (tmpFont == null)
                {
                    Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Failed to create TMP FontAsset from: {filePath}");
                    return false;
                }

                // Set the font asset name for identification
                tmpFont.name = fileName;

                _loadedFonts.Add(tmpFont);
                SetCjkFont(tmpFont);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Failed to load TTF font from {filePath}: {ex.Message}");
                return false;
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

                // Also search for any TTF files that might contain CJK characters
                foreach (string ext in TtfExtensions)
                {
                    string[] files = Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        if (TryLoadTtfFont(file))
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error scanning directory {directory}: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to load a TMP font asset from a file (AssetBundle format).
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
            if (!_cjkFontAvailable || _cjkFontAsset == null) return;

            try
            {
                // Try to find the game's default font (3270-Regular SDF)
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var font in allFonts)
                {
                    if (font != null && font.name != null && font.name.Contains("3270"))
                    {
                        // Add our CJK font as a fallback
                        List<TMP_FontAsset> fallbacks = new List<TMP_FontAsset>(font.fallbackFontAssetTable ?? new List<TMP_FontAsset>());
                        if (!fallbacks.Contains(_cjkFontAsset))
                        {
                            fallbacks.Add(_cjkFontAsset);
                            font.fallbackFontAssetTable = fallbacks;
                            Plugin.LogSource.LogInfo($"[TAIGU] FontManager: Added CJK font as fallback to game font '{font.name}'");
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[TAIGU] FontManager: Error adding fallback font: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the appropriate display label for a region code.
        /// Returns Chinese name if CJK font is available, otherwise returns the code.
        /// </summary>
        public static string GetDisplayLabel(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode))
                return regionCode;

            if (!_cjkFontAvailable)
            {
                // Fallback to ASCII codes if no CJK font
                return regionCode;
            }

            // Map region codes to Chinese names
            switch (regionCode.ToUpper())
            {
                case "CN": return "中国";
                case "TW": return "台湾";
                case "HK": return "香港";
                case "JP": return "日本";
                case "KR": return "韩国";
                case "RU": return "俄罗斯";
                case "VN": return "越南";
                case "TH": return "泰国";
                case "ID": return "印尼";
                case "MY": return "马来";
                case "PH": return "菲律宾";
                case "KH": return "柬埔寨";
                case "LA": return "老挝";
                case "MM": return "缅甸";
                case "MN": return "蒙古";
                case "PL": return "波兰";
                case "CZ": return "捷克";
                case "SK": return "斯洛伐克";
                case "HU": return "匈牙利";
                case "RO": return "罗马尼亚";
                case "BG": return "保加利亚";
                case "UA": return "乌克兰";
                case "TR": return "土耳其";
                case "SA": return "沙特";
                case "IR": return "伊朗";
                case "AE": return "阿联酋";
                case "EG": return "埃及";
                case "MA": return "摩洛哥";
                case "DZ": return "阿尔及利亚";
                case "NG": return "尼日利亚";
                case "ET": return "埃塞俄比亚";
                case "ZA": return "南非";
                case "US": return "美国";
                case "CA": return "加拿大";
                case "MX": return "墨西哥";
                case "BR": return "巴西";
                case "AR": return "阿根廷";
                case "CL": return "智利";
                case "CO": return "哥伦比亚";
                case "PE": return "秘鲁";
                case "DE": return "德国";
                case "FR": return "法国";
                case "ES": return "西班牙";
                case "IT": return "意大利";
                case "PT": return "葡萄牙";
                case "NL": return "荷兰";
                case "SE": return "瑞典";
                case "NO": return "挪威";
                case "DK": return "丹麦";
                case "FI": return "芬兰";
                case "GB": return "英国";
                case "IE": return "爱尔兰";
                case "AU": return "澳大利亚";
                case "NZ": return "新西兰";
                case "IN": return "印度";
                case "PK": return "巴基斯坦";
                case "BD": return "孟加拉";
                case "KZ": return "哈萨克斯坦";
                case "UZ": return "乌兹别克斯坦";
                case "WEST": return "欧美";
                case "EE": case "EAST_EU": return "东欧";
                case "NORD": case "NORDIC": return "北欧";
                case "SEA": return "东南亚";
                case "LATAM": return "拉美";
                case "ME": case "MENA": return "中东";
                case "SAS": case "SAARC": return "南亚";
                case "CAS": case "CENTRAL_ASIA": return "中亚";
                case "NA": return "北非";
                case "SSA": return "撒哈拉以南非洲";
                case "OC": case "OCEANIA": return "大洋洲";
                case "BLT": return "波罗的海";
                case "BALK": return "巴尔干";
                case "IB": return "伊比利亚";
                case "AFRICA": return "非洲";
                case "OTHER": case "Other": return "其他";
                case "??": return "未知";
                default: return regionCode;
            }
        }
    }
}
