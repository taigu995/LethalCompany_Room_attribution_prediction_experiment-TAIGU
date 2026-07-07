// This file is intentionally left minimal.
// LobbySlot UI modification is handled by UI/RegionTagManager.cs
// which uses a MonoBehaviour Update loop to monitor and tag LobbySlots.
//
// The original approach of patching LobbySlot.SetLobbyData failed because
// that method does not exist. The actual LobbySlot structure is:
//   - thisLobby (Lobby) - the lobby data
//   - LobbyName (TextMeshProUGUI) - the display name
//   - lobbyId (SteamId) - the lobby ID
//   - playerCount (TextMeshProUGUI) - player count display
//
// RegionTagManager handles all UI modifications at runtime.

using HarmonyLib;

namespace LethalCompanyRegionTag.Patches
{
    // No patches needed for LobbySlot directly.
    // All UI modification is done by UI.RegionTagManager.
}
