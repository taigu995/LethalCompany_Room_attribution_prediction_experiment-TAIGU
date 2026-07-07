# TAIGU-Room recognition experiment

> Lethal Company 公开房间地区识别模组 / Public Lobby Region Detection Mod v1.5.1
> 署名 / Author: TAIGU

---

## 功能概述 / Overview

在 Lethal Company 的公开房间列表中，自动分析每个房间创建者的国家/地区，并在房间名称前显示地区标签和完整概率分布。

Automatically analyzes the country/region of each lobby creator in Lethal Company's public lobby list, displaying region tags with full probability distribution before the lobby name.

**纯客户端模组**，未安装此模组的人可以正常与安装了此模组的人一起游戏。

**Client-side only mod** - players without this mod can play normally with those who have it installed.

### 示例效果 / Example Display

**中文显示模式 / Chinese Display Mode**（需安装字体 / requires font installation）：
```
[中国] 78% | 日本 7% | 韩国 6% | 其他 5%          小肥羊's Crew        ← 中国玩家创建 / Chinese player
[欧美] 18% | 其他 34% | 美国 18% | 墨西哥 12%     girls only           ← 欧美玩家创建 / Western European player
[俄罗斯] 55% | 乌克兰 12% | 东欧 10% | 其他 10%   альтушка с 3 рязмером ← 俄罗斯玩家创建 / Russian player
[挪威] 91% | 其他 9%                              Dine 800hr+ No reset ← 挪威玩家创建 / Norwegian player
[中国] 78% | 日本 7% | 韩国 6% | 其他 5%          蝙蝠开塞露           ← 中国玩家创建 / Chinese player
```

**英文缩写模式 / Abbreviation Mode**（默认，无需字体 / default, no font needed）：
```
[CN] 78% | JP 7% | KR 6% | OTHER 5%          小肥羊's Crew
[WEST] 18% | OTHER 34% | US 18% | MX 12%     girls only
[RU] 55% | UA 12% | EE 10% | OTHER 10%       альтушка с 3 рязмером
```

标签颜色会根据置信度变化 / Tag color changes based on confidence:
- 🟢 绿色 Green (≥80%): 高置信度 / High confidence
- 🟡 黄绿 Yellow-Green (60-80%): 中高置信度 / Medium-high confidence
- 🟠 橙色 Orange (20-40%): 低置信度 / Low confidence
- ⚪ 灰色 Gray (<20%): 极低置信度 / Very low confidence

---

## 分析方法 / Analysis Methods

模组使用以下 4 种方式（按优先级排序）来判断房间创建者的地区：

The mod uses 4 methods (in priority order) to determine the lobby creator's region:

| 优先级 / Priority | 方法 / Method | 置信度 / Confidence | 需要网络 / Network | 说明 / Description |
|--------|------|--------|-------------|------|
| 1 | Steam Web API | 95% | 需要 API Key / Requires API Key | 最准确 / Most accurate |
| 2 | Steam 社区页面 / Steam Community Page | 88% | 需要 / Required | 解析个人资料页的国家旗帜 / Parses country flag from profile page |
| 3 | Steam XML 资料 / Steam XML Profile | 85% | 需要 / Required | 解析 XML 格式的个人信息 / Parses XML profile data |
| 4 | 昵称语言分析 / Nickname Language Analysis | 50-92% | 不需要 / Not required | 分析 Unicode 字符集和关键词 / Analyzes Unicode character sets and keywords |

### 识别能力 / Detection Capabilities

**文字系统检测 (20+ 种) / Script Detection (20+ scripts)**：

| 文字系统 / Script | 识别地区 / Region | 置信度 / Confidence |
|----------|---------|--------|
| CJK 汉字 / CJK Ideographs | 中国 / China | 78% |
| 日文假名 / Japanese Kana | 日本 / Japan | 92% |
| 韩文 / Korean Hangul | 韩国 / Korea | 92% |
| 西里尔字母 / Cyrillic | 俄罗斯/乌克兰/独联体 / Russia/Ukraine/CIS | 50% |
| 阿拉伯字母 / Arabic | 中东/北非 / Middle East/North Africa | 55% |
| 泰文 / Thai | 泰国 / Thailand | 88% |
| 梵文/天城文 / Devanagari | 印度/尼泊尔 / India/Nepal | 85% |
| 希腊字母 / Greek | 希腊 / Greece | 82% |
| 希伯来字母 / Hebrew | 以色列 / Israel | 85% |
| 老挝文 / Lao | 老挝 / Laos | 85% |
| 高棉文 / Khmer | 柬埔寨 / Cambodia | 85% |
| 缅甸文 / Myanmar | 缅甸 / Myanmar | 85% |
| 蒙古文 / Mongolian | 蒙古 / Mongolia | 80% |
| 越南语特征 / Vietnamese features | 越南 / Vietnam | 75% |
| 土耳其语特征 / Turkish features | 土耳其 / Turkey | 72% |
| 波兰语特征 / Polish features | 波兰 / Poland | 65% |
| 捷克/斯洛伐克语 / Czech/Slovak | 捷克/斯洛伐克 / Czech/Slovakia | 62% |
| 罗马尼亚语 / Romanian | 罗马尼亚 / Romania | 58% |
| 匈牙利语 / Hungarian | 匈牙利 / Hungary | 60% |
| 印尼/马来语 / Indonesian/Malay | 印尼/马来西亚 / Indonesia/Malaysia | 55% |

**关键词识别 (40+ 国家/地区) / Keyword Detection (40+ countries/regions)**：

支持通过房间名中的关键词直接识别国家 / Detects country directly from lobby name keywords:
- 东亚 / East Asia：中国/中国人/国服/日服/한服/중국어
- 东南亚 / Southeast Asia：越南/vietnam/印尼/indonesia/马来/malaysia/philippines
- 欧洲 / Europe：poland/polska/波兰/czech/cesko/hungary/magyar/romania/bulgaria
- 中东/中亚 / Middle East/Central Asia：turkey/turk/arab/iran/saudi/kazakhstan
- 拉美 / Latin America：mexico/brasil/argentina/chile/colombia/peru
- 非洲 / Africa：south africa/morocco/algeria/ethiopia
- 排除词 / Exclusion：no foreigner/no chinese/no russian/internation

---

## 安装方法 / Installation

### 前置要求 / Prerequisites

- Lethal Company 游戏（Steam 版）/ Lethal Company game (Steam version)
- [BepInEx 5.4.x](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) 已安装 / installed

### 安装步骤 / Steps

1. 下载 / Download `LethalCompanyRegionTag.dll`
2. 将文件放入游戏目录下的 `BepInEx/plugins/` 文件夹 / Place the file in `BepInEx/plugins/`:
   ```
   <Steam>\steamapps\common\Lethal Company\BepInEx\plugins\LethalCompanyRegionTag.dll
   ```
3. （可选）安装中文字体以显示中文标签 / (Optional) Install Chinese font for Chinese labels:
   ```
   <Steam>\steamapps\common\Lethal Company\BepInEx\plugins\LethalCompanyRegionTag\chinese_font_ui.ttf
   ```
   将 `chinese_font_ui.ttf`（微软雅黑字体）放入 `BepInEx/plugins/LethalCompanyRegionTag/` 目录 / Place `chinese_font_ui.ttf` (Microsoft YaHei font) in `BepInEx/plugins/LethalCompanyRegionTag/` directory

### 中文字体支持 / Chinese Font Support

模组默认使用 ASCII 缩写显示（如 `[CN]`、`[RU]`）。如需显示中文名称（如 `[中国]`、`[俄罗斯]`），需要安装字体。

The mod defaults to ASCII abbreviations (e.g., `[CN]`, `[RU]`). To display Chinese names (e.g., `[中国]`, `[俄罗斯]`), font installation is required.

**方法 1：手动安装字体（推荐）/ Method 1: Manual font installation (recommended)**
1. 创建目录 / Create directory：`BepInEx/plugins/LethalCompanyRegionTag/`
2. 将 TTF/OTF 格式的中文字体文件放入该目录 / Place TTF/OTF Chinese font file in that directory
3. 模组启动时会自动扫描并加载 / Mod will auto-scan and load on startup

**方法 2：安装汉化模组 / Method 2: Install Chinese localization mod**
- 安装任何包含 CJK 字体的汉化模组，本模组会自动检测并使用其字体 / Install any localization mod with CJK fonts, this mod will auto-detect and use its font

**方法 3：放置字体到游戏根目录 / Method 3: Place font in game root directory**
- 将字体文件放到游戏根目录，模组也会自动扫描 / Place font file in game root directory, mod will auto-scan

### 验证安装 / Verify Installation

启动游戏后，查看 / After launching the game, check `BepInEx/LogOutput.log`:
```
[Info   :   BepInEx] Loading [TAIGU-Room recognition experiment 1.0.0]
[Info   :TAIGU-Room recognition experiment] [TAIGU] Successfully patched: SteamLobbyManagerPatch
[Info   :TAIGU-Room recognition experiment] [TAIGU] TAIGU-Room recognition experiment v1.0.0 loaded!
[Info   :TAIGU-Room recognition experiment] [TAIGU] Region detection enabled: Nickname=True, SteamAPI=False
[Info   :TAIGU-Room recognition experiment] [TAIGU] RegionTagManager initialized
```

---

## 配置说明 / Configuration

首次运行后，会在 / After first run, config file is generated at `BepInEx/config/TAIGU.RoomRecognition.cfg`:

```ini
[Analysis Sources]
## 启用昵称语言分析（无需网络，始终可用）
## Enable nickname language analysis (no network required, always available)
EnableNicknameAnalysis = true

## 启用 Steam 社区页面查询（无需 API Key）
## Enable Steam Community page query (no API Key required)
EnableCommunityQuery = true

## 启用 Steam XML 资料查询（兜底方案）
## Enable Steam XML profile query (fallback method)
EnableXmlQuery = true

## Steam Web API Key（可选，最准确）
## Steam Web API Key (optional, most accurate)
## 从 / Get from: https://steamcommunity.com/dev/apikey
SteamWebApiKey = 

[Display Settings]
## 最低置信度阈值（低于此值不显示标签）
## Minimum confidence threshold (tags below this won't be shown)
MinConfidenceThreshold = 20

## 是否显示无法识别地区的标签 [??]
## Whether to show tags for unrecognized regions [??]
ShowLowConfidenceTags = false

## 是否显示概率百分比
## Whether to show probability percentages
ShowProbability = true

## 是否使用中文显示地区名称（需要 CJK 字体支持）
## Whether to use Chinese for region names (requires CJK font support)
## true: 显示 / Shows [中国] [日本] [俄罗斯] etc.
## false: 显示 / Shows [CN] [JP] [RU] etc.
UseChineseDisplay = true
```

### 获取 Steam Web API Key（可选但推荐）/ Get Steam Web API Key (optional but recommended)

1. 登录 / Log in to Steam Community: https://steamcommunity.com/dev/apikey
2. 填写域名（可填任意域名如 / Fill domain name (any domain like `localhost`)）和密钥名称 / and key name
3. 复制生成的 Key，粘贴到配置文件的 / Copy the generated Key, paste into config file `SteamWebApiKey` field

配置 API Key 后，地区识别准确率可提升至 95%+。/ With API Key configured, region detection accuracy can reach 95%+.

---

## 技术架构 / Technical Architecture

```
LethalCompanyRegionTag/
├── Plugin.cs                      # BepInEx 插件入口 / Plugin entry point
├── Analysis/
│   ├── RegionAnalyzer.cs          # 多源综合分析引擎 / Multi-source analysis engine
│   ├── NicknameAnalyzer.cs        # 昵称 Unicode 字符集分析 / Nickname Unicode analysis
│   ├── SteamWebQuery.cs           # Steam Web API / 社区 / XML 查询 / Steam queries
│   └── RegionResult.cs            # 分析结果数据模型 / Analysis result model
├── Patches/
│   └── LobbySlotPatch.cs          # Harmony Patch (Hook 房间列表 / Hook lobby list)
├── UI/
│   ├── FontManager.cs             # 字体管理器（扫描/加载 CJK 字体）/ Font manager
│   └── RegionTagManager.cs        # UI 标签管理 / UI tag management
├── Cache/
│   └── RegionCache.cs             # 线程安全 TTL 缓存 / Thread-safe TTL cache
└── Config/
    └── PluginConfig.cs            # BepInEx 配置管理 / BepInEx config management
```

### 工作原理 / How It Works

1. **Hook 房间列表 / Hook lobby list**：通过 Harmony Patch 拦截 / Intercepts `SteamLobbyManager.loadLobbyListAndFilter` via Harmony Patch
2. **Phase 1 - 即时分析 / Instant analysis**：从 / Reads server name from `LobbySlot.LobbyName`，使用 Unicode 字符集分析 + 关键词匹配 / uses Unicode analysis + keyword matching
3. **Phase 2 - 异步查询 / Async query**：后台从 / Background extracts lobby owner Steam ID from `Lobby.Owner.Id`，异步查询 Steam 社区页面 / async queries Steam Community page
4. **标签更新 / Tag update**：Steam 查询完成后自动更新标签 / Auto-updates tag after Steam query completes，带 `*` 标记表示已验证 / `*` mark indicates verified
5. **字体加载 / Font loading**：FontManager 自动扫描并加载 CJK 字体 / Auto-scans and loads CJK fonts
6. **结果缓存 / Result caching**：分析结果缓存 10 分钟 / Analysis results cached for 10 minutes

### 兼容性 / Compatibility

- **纯客户端 / Client-side only**：不修改游戏逻辑，不影响联机 / Doesn't modify game logic, doesn't affect multiplayer
- **无 API Key 也能工作 / Works without API Key**：昵称分析作为兜底方案始终可用 / Nickname analysis always available as fallback
- **字体兼容 / Font compatible**：支持自动加载 CJK 字体，显示中文标签 / Supports auto-loading CJK fonts for Chinese labels
- **MoreCompany 兼容 / MoreCompany compatible**：与多人房间扩展模组完全兼容 / Fully compatible with player count extension mod

---

## 注意事项 / Notes

- Steam 注册地区 ≠ 实际国籍 / Steam registered region ≠ actual nationality（用户可自由设置 / users can set freely）
- 昵称分析基于统计规律，不是 100% 准确 / Nickname analysis is based on statistical patterns, not 100% accurate
- Steam 用户隐私设置可能导致部分地区信息无法获取 / Steam privacy settings may prevent some region info from being retrieved
- 网络查询有 8 秒超时，不会阻塞游戏 / Network queries have 8-second timeout, won't block the game
- 中文标签显示需要安装 CJK 字体（见安装步骤），否则自动降级为英文缩写 / Chinese labels require CJK font installation (see installation steps), otherwise falls back to English abbreviations

---

## 版本历史 / Changelog

### v1.5.1 - 地区代码映射修复 / Region Code Mapping Fix
- 修复 `RegionResult.GetRegionCode` 代码映射不一致导致中文显示失败的问题 / Fixed inconsistent code mapping in `RegionResult.GetRegionCode` causing Chinese display failure
- 统一三个独立的 `GetRegionCode` 方法返回一致的代码 / Unified three separate `GetRegionCode` methods to return consistent codes
- 修复 "Western Europe" 显示为 `[EU]` 而非 `[欧美]` 的问题 / Fixed "Western Europe" displaying as `[EU]` instead of `[欧美]`
- 修复 "Central Asia" 显示为 `[加拿大]` 而非 `[中亚]` 的问题 / Fixed "Central Asia" displaying as `[加拿大]` instead of `[中亚]`
- 修复 "North America" 显示为 `[美国]` 而非 `[北美]` 的问题 / Fixed "North America" displaying as `[美国]` instead of `[北美]`
- 修复 "Other CIS"、"Balkans" 等区域代码无中文映射的问题 / Fixed missing Chinese mappings for "Other CIS", "Balkans" and other regions
- 修复字典大小写重复键导致构建失败的问题 / Fixed case-insensitive duplicate key causing dictionary build failure

### v1.5.0 - 字体管理器 / Font Manager
- 新增 FontManager 模块，自动扫描并加载 CJK 字体 / Added FontManager module, auto-scans and loads CJK fonts
- 支持从插件目录加载 TTF/OTF 字体文件 / Supports loading TTF/OTF font files from plugin directory
- 支持 5 层字体扫描策略 / 5-layer font scanning strategy（插件目录→已加载字体→游戏目录→BepInEx→根目录 / plugin dir → loaded fonts → game dir → BepInEx → root dir）
- 标签自动使用加载的字体显示中文 / Tags automatically use loaded font for Chinese display

### v1.4.0 - Steam 自动查询 / Steam Auto Query
- 两阶段识别策略 / Two-phase detection strategy：Phase 1 即时分析 + Phase 2 异步 Steam 查询 / instant analysis + async Steam query
- 自动从 Lobby 对象提取房主 Steam ID / Auto-extracts lobby owner Steam ID from Lobby object
- Steam 社区页面查询结果自动更新标签 / Steam Community page query results auto-update tags

### v1.3.0 - 中文 UI 显示 / Chinese UI Display
- 新增中文地区名称显示 / Added Chinese region name display（如 / e.g., `[中国]` 代替 / instead of `[CN]`）
- 新增 / Added `UseChineseDisplay` 配置选项 / config option
- 覆盖 60+ 个国家/地区的中文名称映射 / Covers 60+ country/region Chinese name mappings

### v1.2.0 - 识别算法优化 / Detection Algorithm Optimization
- 扩展关键词数据库 / Expanded keyword database（40+ 国家/地区 / countries/regions）
- 新增拉丁文字语言特征分析 / Added Latin script language feature analysis（土耳其语、波兰语、捷克语等 / Turkish, Polish, Czech, etc.）
- 新增 Unicode 文字系统检测 / Added Unicode script detection（老挝文、高棉文、缅甸文、蒙古文 / Lao, Khmer, Myanmar, Mongolian）
- 优化置信度评分和概率分布算法 / Optimized confidence scoring and probability distribution algorithm

### v1.1.0 - 初始版本 / Initial Release
- 多源混合分析 / Multi-source hybrid analysis（Steam API + 社区页面 / Community page + XML + 昵称分析 / nickname analysis）
- 完整概率分布显示 / Full probability distribution display
- BepInEx 配置支持 / BepInEx configuration support

---

## 文件清单 / File List

| 文件 / File | 大小 / Size | 说明 / Description |
|------|------|------|
| `LethalCompanyRegionTag.dll` | 95KB | 主模组 DLL / Main mod DLL |
| `chinese_font_ui.ttf` | 19MB | 微软雅黑字体（可选，用于显示中文）/ Microsoft YaHei font (optional, for Chinese display) |
| `TAIGU-RoomRecognition-README.md` | - | 本说明文件 / This readme file |

---

## 许可 / License

仅供学习交流使用。/ For learning and communication purposes only.
