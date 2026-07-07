# TAIGU-Room recognition experiment

> Lethal Company 公开房间地区识别模组 v1.5.0
> 署名: TAIGU

## 功能概述

在 Lethal Company 的公开房间列表中，自动分析每个房间创建者的国家/地区，并在房间名称前显示地区标签和完整概率分布。

**纯客户端模组**，未安装此模组的人可以正常与安装了此模组的人一起游戏。

### 示例效果

**中文显示模式**（需安装字体）：
```
[中国 78% | 日本 8% | 韩国 7% | 越南 4%] 来几个人啊起嘛        ← 中国玩家创建
[日本 92% | 中国 4% | 欧美 4%]            たのしいゲーム        ← 日本玩家创建
[俄罗斯 85% | 乌克兰 8% | 白俄罗斯 4%]    Игровой сервер        ← 俄罗斯玩家创建
[波兰 65% | 德国 15% | 欧美 12%]          poland polska pl       ← 波兰玩家创建
[欧美 35% | 德国 20% | 法国 15%]          Cool Lobby             ← 西欧玩家创建
```

**英文缩写模式**（默认，无需字体）：
```
[CN 78% | JP 8% | KR 7% | VI 4%] 来几个人啊起嘛
[JP 92% | CN 4% | WEST 4%]            たのしいゲーム
[RU 85% | UA 8% | BY 4%]              Игровой сервер
```

标签颜色会根据置信度变化：
- 🟢 绿色 (≥80%): 高置信度
- 🟡 黄绿 (60-80%): 中高置信度
- 🟠 橙色 (20-40%): 低置信度
- ⚪ 灰色 (<20%): 极低置信度

## 分析方法（多源混合）

模组使用以下 4 种方式（按优先级排序）来判断房间创建者的地区：

| 优先级 | 方法 | 置信度 | 是否需要网络 | 说明 |
|--------|------|--------|-------------|------|
| 1 | Steam Web API | 95% | 需要 API Key | 最准确，需配置免费 API Key |
| 2 | Steam 社区页面 | 88% | 需要网络 | 解析个人资料页的国家旗帜 |
| 3 | Steam XML 资料 | 85% | 需要网络 | 解析 XML 格式的个人信息 |
| 4 | 昵称语言分析 | 50-92% | 不需要 | 分析昵称中的 Unicode 字符集和关键词 |

### v1.1.0 识别能力提升

**新增/增强的文字系统检测 (20+ 种)**：

| 文字系统 | 识别地区 | 置信度 |
|----------|---------|--------|
| CJK 汉字 | 中国 | 78% |
| 日文假名 (平假名/片假名) | 日本 | 92% |
| 韩文 (谚文 + 古谚文) | 韩国 | 92% |
| 西里尔字母 | 俄罗斯/乌克兰/独联体 | 50% |
| 阿拉伯字母 | 中东/北非 | 55% |
| 泰文 | 泰国 | 88% |
| 梵文/天城文 | 印度/尼泊尔 | 85% |
| 希腊字母 | 希腊 | 82% |
| 希伯来字母 | 以色列 | 85% |
| 亚美尼亚字母 | 亚美尼亚 | 80% |
| 格鲁吉亚字母 | 格鲁吉亚 | 82% |
| 老挝文 | 老挝 | 85% |
| 高棉文 | 柬埔寨 | 85% |
| 缅甸文 | 缅甸 | 85% |
| 蒙古文 | 蒙古 | 80% |
| 埃塞俄文 (Ge'ez) | 埃塞俄比亚 | 80% |
| 越南语特征 | 越南 | 75% |
| 土耳其语特征 | 土耳其 | 72% |
| 波兰语特征 | 波兰 | 65% |
| 捷克/斯洛伐克语特征 | 捷克/斯洛伐克 | 62% |
| 罗马尼亚语特征 | 罗马尼亚 | 58% |
| 匈牙利语特征 | 匈牙利 | 60% |
| 印尼/马来语特征 | 印尼/马来西亚 | 55% |
| 斯堪的纳维亚特征 | 北欧 | 55% |
| 西班牙语模式 | 西班牙/拉美 | 25-35% |
| 德语模式 | 德国/奥地利 | 45% |
| 法语模式 | 法国 | 40% |
| 葡萄牙语特征 | 巴西/葡萄牙 | 45% |
| 意大利语特征 | 意大利 | 42% |

**新增关键词识别 (40+ 国家/地区)**：

支持通过房间名中的关键词直接识别国家，包括：
- 东亚：中国/中国人/国服/日服/한服/중국어 等
- 东南亚：越南/vietnam/印尼/indonesia/马来/malaysia/philippines 等
- 欧洲：poland/polska/波兰/czech/cesko/hungary/magyar/romania/romania/bulgaria 等
- 中东/中亚：turkey/turk/arab/iran/saudi/kazakhstan 等
- 拉美：mexico/brasil/argentina/chile/colombia/peru 等
- 非洲：south africa/morocco/algeria/ethiopia 等
- 排除词：no foreigner/no chinese/no russian/internation 等

## 安装方法

### 前置要求

- Lethal Company 游戏（Steam 版）
- [BepInEx 5.4.x](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) 已安装

### 安装步骤

1. 下载 `LethalCompanyRegionTag.dll`
2. 将文件放入游戏目录下的 `BepInEx/plugins/` 文件夹：
   ```
   <Steam>\steamapps\common\Lethal Company\BepInEx\plugins\LethalCompanyRegionTag.dll
   ```
3. （可选）安装中文字体以显示中文标签：
   ```
   <Steam>\steamapps\common\Lethal Company\BepInEx\plugins\LethalCompanyRegionTag\chinese_font_ui.ttf
   ```
   将 `chinese_font_ui.ttf`（微软雅黑字体）放入 `BepInEx/plugins/LethalCompanyRegionTag/` 目录

### 中文字体支持

模组默认使用 ASCII 缩写显示（如 `[CN]`、`[RU]`）。如需显示中文名称（如 `[中国]`、`[俄罗斯]`），需要安装字体：

**方法 1：手动安装字体（推荐）**
1. 创建目录：`BepInEx/plugins/LethalCompanyRegionTag/`
2. 将 TTF/OTF 格式的中文字体文件放入该目录
3. 模组启动时会自动扫描并加载

**方法 2：安装汉化模组**
- 安装任何包含 CJK 字体的汉化模组，本模组会自动检测并使用其字体

**方法 3：放置字体到游戏根目录**
- 将字体文件放到游戏根目录，模组也会自动扫描
3. 启动游戏，模组自动加载

### 验证安装

启动游戏后，查看 `BepInEx/LogOutput.log`，应看到：
```
[Info   :   BepInEx] Loading [TAIGU-Room recognition experiment 1.0.0]
[Info   :TAIGU-Room recognition experiment] [TAIGU] Successfully patched: SteamLobbyManagerPatch
[Info   :TAIGU-Room recognition experiment] [TAIGU] TAIGU-Room recognition experiment v1.0.0 loaded!
[Info   :TAIGU-Room recognition experiment] [TAIGU] Region detection enabled: Nickname=True, SteamAPI=False
[Info   :TAIGU-Room recognition experiment] [TAIGU] RegionTagManager initialized
```

## 配置说明

首次运行后，会在 `BepInEx/config/TAIGU.RoomRecognition.cfg` 生成配置文件：

```ini
[Analysis Sources]
## 启用昵称语言分析（无需网络，始终可用）
EnableNicknameAnalysis = true

## 启用 Steam 社区页面查询（无需 API Key）
EnableCommunityQuery = true

## 启用 Steam XML 资料查询（兜底方案）
EnableXmlQuery = true

## Steam Web API Key（可选，最准确）
## 从 https://steamcommunity.com/dev/apikey 免费获取
SteamWebApiKey = 

[Display Settings]
## 最低置信度阈值（低于此值不显示标签）
MinConfidenceThreshold = 20

## 是否显示无法识别地区的标签 [??]
ShowLowConfidenceTags = false

## 是否显示概率百分比
ShowProbability = true

## 是否使用中文显示地区名称（需要 CJK 字体支持）
## true: 显示 [中国] [日本] [俄罗斯] 等
## false: 显示 [CN] [JP] [RU] 等
UseChineseDisplay = true
```

### 获取 Steam Web API Key（可选但推荐）

1. 登录 Steam 社区：https://steamcommunity.com/dev/apikey
2. 填写域名（可填任意域名如 `localhost`）和密钥名称
3. 复制生成的 Key，粘贴到配置文件的 `SteamWebApiKey` 字段

配置 API Key 后，地区识别准确率可提升至 95%+。

## 技术架构

```
LethalCompanyRegionTag/
├── Plugin.cs                      # BepInEx 插件入口
├── Analysis/
│   ├── RegionAnalyzer.cs          # 多源综合分析引擎
│   ├── NicknameAnalyzer.cs        # 昵称 Unicode 字符集分析
│   ├── SteamWebQuery.cs           # Steam Web API / 社区 / XML 查询
│   └── RegionResult.cs            # 分析结果数据模型
├── Patches/
│   └── LobbySlotPatch.cs          # Harmony Patch (Hook 房间列表)
├── UI/
│   ├── FontManager.cs             # 字体管理器（扫描/加载 CJK 字体）
│   └── RegionTagManager.cs        # UI 标签管理
├── Cache/
│   └── RegionCache.cs             # 线程安全 TTL 缓存
└── Config/
    └── PluginConfig.cs            # BepInEx 配置管理
```

### 工作原理

1. **Hook 房间列表**：通过 Harmony Patch 拦截 `SteamLobbyManager.loadLobbyListAndFilter`，获取公开房间列表
2. **Phase 1 - 即时分析**：从 `LobbySlot.LobbyName` 读取服务器名称，使用 Unicode 字符集分析 + 关键词匹配，立即显示初步标签
3. **Phase 2 - 异步查询**：后台从 `Lobby.Owner.Id` 提取房主 Steam ID，异步查询 Steam 社区页面获取真实国家代码
4. **标签更新**：Steam 查询完成后自动更新标签，带 `*` 标记表示已验证
5. **字体加载**：FontManager 自动扫描并加载 CJK 字体，支持中文标签显示
6. **结果缓存**：分析结果缓存 10 分钟，避免重复查询

### 兼容性

- **纯客户端**：不修改游戏逻辑，不影响联机
- **无 API Key 也能工作**：昵称分析作为兜底方案始终可用
- **字体兼容**：支持自动加载 CJK 字体，显示中文标签
- **MoreCompany 兼容**：与多人房间扩展模组完全兼容

## 注意事项

- Steam 注册地区 ≠ 实际国籍（用户可自由设置）
- 昵称分析基于统计规律，不是 100% 准确
- Steam 用户隐私设置可能导致部分地区信息无法获取
- 网络查询有 8 秒超时，不会阻塞游戏
- 中文标签显示需要安装 CJK 字体（见安装步骤），否则自动降级为英文缩写

## 版本历史

### v1.5.0 - 字体管理器
- 新增 FontManager 模块，自动扫描并加载 CJK 字体
- 支持从插件目录加载 TTF/OTF 字体文件
- 支持 5 层字体扫描策略（插件目录→已加载字体→游戏目录→BepInEx→根目录）
- 标签自动使用加载的字体显示中文

### v1.4.0 - Steam 自动查询
- 两阶段识别策略：Phase 1 即时分析 + Phase 2 异步 Steam 查询
- 自动从 Lobby 对象提取房主 Steam ID
- Steam 社区页面查询结果自动更新标签

### v1.3.0 - 中文 UI 显示
- 新增中文地区名称显示（如 `[中国]` 代替 `[CN]`）
- 新增 `UseChineseDisplay` 配置选项
- 覆盖 60+ 个国家/地区的中文名称映射

### v1.2.0 - 识别算法优化
- 扩展关键词数据库（40+ 国家/地区）
- 新增拉丁文字语言特征分析（土耳其语、波兰语、捷克语等）
- 新增 Unicode 文字系统检测（老挝文、高棉文、缅甸文、蒙古文）
- 优化置信度评分和概率分布算法

### v1.1.0 - 初始版本
- 多源混合分析（Steam API + 社区页面 + XML + 昵称分析）
- 完整概率分布显示
- BepInEx 配置支持

## 文件清单

| 文件 | 大小 | 说明 |
|------|------|------|
| `LethalCompanyRegionTag.dll` | 90KB | 主模组 DLL |
| `chinese_font_ui.ttf` | 19MB | 微软雅黑字体（可选，用于显示中文） |
| `TAIGU-RoomRecognition-README.md` | - | 本说明文件 |

## 许可

仅供学习交流使用。
