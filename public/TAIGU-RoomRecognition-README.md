# TAIGU-Room recognition experiment

Lethal Company 公开房间地区识别模组 - 通过多源分析识别房间创建者的国家/地区，并在房间列表中显示标签。

## 功能特性

- **多源混合分析**：结合昵称语言分析、Steam Web API、Steam 社区页面、Steam XML 配置等多种数据源
- **概率评估**：显示分析为某国的概率百分比
- **Unicode 字符集分析**：支持识别中文、日文假名、韩文、西里尔字母、阿拉伯文、泰文、梵文、希腊文、希伯来文等 20+ 种文字系统
- **拉丁字母智能分析**：通过常见姓名模式识别西班牙语、德语、法语、越南语、土耳其语、波兰语、斯堪的纳维亚语等
- **彩色标签**：不同地区使用不同颜色标注（中国红色、日本粉色、韩国蓝色、俄罗斯橙色等）
- **缓存机制**：分析结果缓存 10 分钟，避免重复查询
- **纯客户端模组**：不影响联机，未安装此模组的人可以和安装此模组的人一起游戏

## 安装步骤

### 前置要求

1. **BepInEx 5.4.x** - Lethal Company 的 Mod 加载框架
   - 下载地址: https://github.com/BepInEx/BepInEx/releases
   - 选择 `BepInEx_win_x64_5.4.21.0.zip`
   - 解压到 Lethal Company 游戏根目录

2. **确认 BepInEx 已正确安装**
   - 启动一次游戏后关闭，确认 `BepInEx` 文件夹已生成

### 安装模组

1. 将 `LethalCompanyRegionTag.dll` 复制到以下目录：
   ```
   <Lethal Company 游戏目录>/BepInEx/plugins/
   ```

2. 完整路径示例：
   ```
   Steam\steamapps\common\Lethal Company\BepInEx\plugins\LethalCompanyRegionTag.dll
   ```

3. 启动游戏，模组会自动加载

## 配置说明

首次启动后，配置文件会自动生成在：
```
<Lethal Company 游戏目录>/BepInEx/config/TAIGU.RoomRecognition.cfg
```

### 关键配置项

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| **Steam API > WebApiKey** | (空) | Steam Web API 密钥，获取地址: https://steamcommunity.com/dev/apikey |
| **Steam API > EnableCommunityQuery** | true | 启用 Steam 社区页面查询（无需 API Key） |
| **Steam API > EnableXmlQuery** | true | 启用 Steam XML 配置查询 |
| **Display > ShowRegionTag** | true | 显示地区标签 |
| **Display > ShowProbability** | true | 显示概率百分比 |
| **Display > ShowCountryCode** | true | 显示国家代码 (如 [CN], [JP]) |
| **Display > TagFontSize** | 14 | 标签字体大小 |
| **Analysis > CacheTtlMinutes** | 10 | 缓存过期时间（分钟） |
| **Debug > DebugLogging** | false | 启用调试日志 |

### 获取 Steam Web API Key（可选但推荐）

1. 访问 https://steamcommunity.com/dev/apikey
2. 登录你的 Steam 账号
3. 填写域名信息（随意填写即可）
4. 获取 API Key
5. 将 Key 填入配置文件的 `WebApiKey` 字段

配置 API Key 后，地区识别准确率会显著提升。

## 分析原理

### 数据源优先级

1. **Steam Web API** (置信度 88-98%)
   - 通过 `ISteamUser/GetPlayerSummaries/v2/` 获取 `loccountrycode`
   - 需要 API Key
   - 受用户隐私设置影响

2. **Steam 社区页面** (置信度 85-90%)
   - 解析 Steam 社区个人资料页面的国旗标识
   - 无需 API Key，但可能被限流

3. **Steam XML 配置** (置信度 85-90%)
   - 解析 Steam 用户 XML 个人资料
   - 无需 API Key

4. **昵称语言分析** (置信度 50-95%)
   - 分析昵称中的 Unicode 字符集分布
   - 100% 可用，无需网络
   - 准确率取决于昵称特征

### 昵称分析规则

| 字符集 | 判断结果 | 置信度 |
|--------|----------|--------|
| 日文假名 (ひらがな/カタカナ) | 日本 | 92% |
| 韩文 (한글) | 韩国 | 92% |
| CJK 汉字 (无假名/韩文) | 中国 | 78% |
| 西里尔字母 | 俄罗斯/独联体 | 50% |
| 阿拉伯文 | 中东/北非 | 55% |
| 泰文 | 泰国 | 88% |
| 梵文 (天城文) | 印度 | 85% |
| 希腊文 | 希腊 | 82% |
| 希伯来文 | 以色列 | 85% |
| 越南语特征字符 | 越南 | 75% |
| 土耳其语特征字符 | 土耳其 | 72% |
| 波兰语特征字符 | 波兰 | 65% |
| 拉丁字母 (通用) | 西欧/北美 | 20% |

## 颜色方案

| 地区 | 颜色 |
|------|------|
| 中国 | 红色 #FF4444 |
| 日本 | 粉色 #FF69B4 |
| 韩国 | 蓝色 #4169E1 |
| 俄罗斯/独联体 | 橙色 #FF8C00 |
| 东南亚 | 绿色 #22AA44 |
| 印度/南亚 | 橙黄 #FF9933 |
| 中东/北非 | 深绿 #00AA00 |
| 土耳其 | 红色 #E30A17 |
| 巴西/拉美 | 绿色 #009C3B |
| 德国 | 黄色 #FFCC00 |
| 法国 | 蓝色 #0055A4 |
| 英国 | 深蓝 #003078 |
| 北欧 | 亮蓝 #0066CC |
| 北美 | 靛蓝 #3C3B6E |
| 其他 | 灰色 #AAAAAA |

## 兼容性

- **游戏版本**: Lethal Company v50+ (v56 测试通过)
- **BepInEx**: 5.4.x
- **联机兼容**: 完全兼容，纯客户端 UI 修改
- **其他 Mod**: 与大多数 Mod 兼容

## 已知限制

1. **隐私设置**: 如果 Steam 用户将个人资料设为私密，`loccountrycode` 可能为空
2. **昵称误导**: 用户可能使用其他语言的昵称（如中国用户使用英文名）
3. **Steam 注册地区**: Steam 注册地区不等于实际国籍
4. **网络延迟**: Steam API 查询可能因网络问题失败
5. **游戏更新**: 游戏大版本更新可能导致 Hook 失效

## 署名

**TAIGU-Room recognition experiment**

## 免责声明

本模组仅供学习和研究使用。地区识别结果基于公开数据推断，不保证 100% 准确。Steam 注册地区不代表用户的实际国籍或所在地。
