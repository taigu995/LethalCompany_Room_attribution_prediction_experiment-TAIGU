# AGENTS.md - LethalCompanyRegionTag

## Project Overview
BepInEx plugin for Lethal Company that identifies lobby host country/region using multi-source analysis (Steam API + nickname language detection).

## Build Commands
```bash
cd /workspace/projects/LethalCompanyRegionTag
dotnet restore
dotnet build -c Release
```

## Output
- DLL: `bin/Release/netstandard2.1/LethalCompanyRegionTag.dll`
- Install to: `<Lethal Company>/BepInEx/plugins/`

## Key Files
- `Plugin.cs` - BepInEx entry point, Harmony initialization
- `Analysis/NicknameAnalyzer.cs` - Unicode character set analysis for region detection
- `Analysis/SteamWebQuery.cs` - Steam Web API / Community page / XML queries
- `Analysis/RegionAnalyzer.cs` - Multi-source analysis engine with probability merging
- `Analysis/RegionResult.cs` - Result data model
- `Patches/SteamLobbyManagerPatch.cs` - Hook SteamLobbyManager.LoadServerList / loadLobbyListAndFilter
- `Patches/LobbySlotPatch.cs` - Hook LobbySlot UI to add region tags
- `Cache/RegionCache.cs` - Thread-safe TTL cache for analysis results
- `Config/PluginConfig.cs` - BepInEx configuration entries

## Dependencies (NuGet)
- BepInEx.Core 5.4.21 (from nuget.bepinex.dev)
- Lib.Harmony 2.2.2
- LethalCompany.GameLibs.Steam 56.0.0-beta.0-ngd.0
- UnityEngine.Modules 2022.3.9
- UnityEngine.UI (from GameLibs)
- Unity.TextMeshPro (from GameLibs)

## Game Architecture Notes
- Game uses **Facepunch.Steamworks** (namespace: `Steamworks`, `Steamworks.Data`, `Steamworks.ServerList`)
- Key game classes: `SteamLobbyManager`, `LobbySlot`, `GameNetworkManager`, `QuickMenuManager`, `StartOfRound`
- Lobby list managed by `SteamLobbyManager.LoadServerList()` and `loadLobbyListAndFilter()`
- Each lobby entry rendered by `LobbySlot` MonoBehaviour
- Game uses TextMeshPro for UI text

## Plugin GUID
`TAIGU.RoomRecognition`
