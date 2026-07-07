# TAIGU-Room recognition experiment

> Lethal Company 公开房间地区识别模组 v1.1.0
> 署名: TAIGU

## 功能概述

在 Lethal Company 的公开房间列表中，自动分析每个房间创建者的国家/地区，并在房间名称前显示地区标签和完整概率分布。

**纯客户端模组**，未安装此模组的人可以正常与安装了此模组的人一起游戏。

### 示例效果

```
[CN 78% | JP 8% | KR 7% | VI 4%] 来几个人啊起嘛        ← 中国玩家创建
[JP 92% | CN 4% | WEST 4%]     たのしいゲーム        ← 日本玩家创建
[RU 85% | UA 8% | BY 4%]       Игровой сервер        ← 俄罗斯玩家创建
[PL 65% | DE 15% | WEST 12%]   poland polska pl       ← 波兰玩家创建
[WEST 35% | DE 20% | FR 15%]   Cool Lobby             ← 西欧玩家创建
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
│   └── SteamLobbyManagerPatch.cs  # Harmony Patch (Hook 房间列表)
├── UI/
│   └── RegionTagManager.cs        # UI 标签管理 MonoBehaviour
├── Cache/
│   └── RegionCache.cs             # 线程安全 TTL 缓存
└── Config/
    └── PluginConfig.cs            # BepInEx 配置管理
```

### 工作原理

1. **Hook 房间列表**：通过 Harmony Patch 拦截 `SteamLobbyManager.loadLobbyListAndFilter`，获取公开房间列表
2. **获取房主信息**：通过 `Lobby.Owner` 获取房主的 `Friend` 对象（包含昵称和 SteamID）
3. **多源分析**：按优先级依次尝试 Steam Web API → 社区页面 → XML 资料 → 昵称分析
4. **UI 标注**：在 `LobbySlot.LobbyName` 前添加 ASCII 格式的地区标签（如 `[CN 78%]`）
5. **结果缓存**：分析结果缓存 10 分钟，避免重复查询

### 兼容性

- **纯客户端**：不修改游戏逻辑，不影响联机
- **无 API Key 也能工作**：昵称分析作为兜底方案始终可用
- **字体兼容**：标签使用纯 ASCII 字符，兼容游戏默认字体

## 注意事项

- Steam 注册地区 ≠ 实际国籍（用户可自由设置）
- 昵称分析基于统计规律，不是 100% 准确
- Steam 用户隐私设置可能导致部分地区信息无法获取
- 网络查询有 8 秒超时，不会阻塞游戏

## 许可

仅供学习交流使用。
