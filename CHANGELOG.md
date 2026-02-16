# Changelog

All notable changes to this project will be documented in this file.

## [4.0.0] - 2026-02-16

### Breaking Changes
- `MembershipDetails.LevelName` is now treated as membership tier information when available (for example from welcome subtext runs), instead of reflecting badge tenure labels.
- New-member membership events now preserve welcome message runs in `ChatItem.Message` (`Welcome to {tier}!`) where available. Consumers previously assuming empty message arrays for new-member events must adjust.

### Added
- Added raw action surface: `IYTLiveChat.RawActionReceived` with `RawActionReceivedEventArgs.RawAction` and optional `ParsedChatItem` mapping.
- Added `ChatItem.ViewerLeaderboardRank` extraction for YouTube points leaderboard rank tags (for example `#1`, `#2`, `#3`).
- Added `MembershipDetails.MembershipBadgeLabel` to preserve badge/tenure text separately from tier name.
- Added `ChatItem.IsTicker` to indicate events sourced from `addLiveChatTickerItemAction`.
- Added ticker event parsing support:
  - `liveChatTickerPaidMessageItemRenderer` -> parsed super chat item.
  - `liveChatTickerSponsorItemRenderer` -> parsed membership item.
- Added ticker gift purchase mapping support:
  - `liveChatTickerSponsorItemRenderer` nested `liveChatSponsorshipsGiftPurchaseAnnouncementRenderer` -> parsed membership gift purchase item.
  - `liveChatTickerPaidStickerItemRenderer` nested `liveChatPaidStickerRenderer` -> parsed sticker superchat item.
- Added raw-log based test fixtures for ticker action schemas (`RawActionTestData`).
- Added additional unsanitized raw fixtures from `log9`-`log15` (`RawActionLog9To15TestData`) for:
  - `updateLiveChatPollAction`
  - `showLiveChatActionPanelAction`
  - `addBannerToLiveChatCommand`
  - ticker gift purchases
  - new membership welcome-tier event samples
- Added async stream helpers:
  - `IYTLiveChat.StreamChatItemsAsync(...)`
  - `IYTLiveChat.StreamRawActionsAsync(...)`
- Added lightweight log analysis utility project (`YTLiveChat.Tools`) to inspect action/renderer distributions in captured logs.
- Added continuous livestream monitor mode (BETA/UNSUPPORTED):
  - `YTLiveChatOptions.EnableContinuousLivestreamMonitor`
  - `YTLiveChatOptions.LiveCheckFrequency`
  - lifecycle events `LivestreamStarted` and `LivestreamEnded`
- Example console app upgraded into a one-line colorized TUI view with UTF-8 output, rank tags, membership/superchat tags, raw unsupported action hints, emoji and badge display.

### Changed
- Unified currency parsing in dedicated `CurrencyParser` helper using CLDR-style symbols plus closure-style fallback mappings and ISO fallback handling.
- Expanded currency parsing coverage for more YouTube formats (including prefixed dollar forms and additional ISO code passthrough behavior).
- Debug raw JSON capture now writes a valid JSON array file structure (instead of loose pretty-printed objects), suitable for downstream tooling.
- Service response handling now maps parsed items to source action indices and can emit both parsed events and raw action events from one response pass.
- Added CI workflow matrix for `net8.0` and `net10.0` test runs and repository-wide build analysis settings via `Directory.Build.props`.
- Added `logs/` ignore pattern and moved local capture logs under that folder for cleaner working trees.

### Performance / Modernization
- Kept compile-time regex generation for modern TFMs (`GeneratedRegex`) with compatibility fallbacks for `netstandard` targets.
- Reduced allocations in parser hot paths by replacing several LINQ-heavy paths with explicit loops and pre-sized collections.
- Added net10+ optimized currency lookup path via frozen dictionaries while preserving netstandard-compatible dictionary fallback.
- Pre-sized internal collections for parsed action indexing and raw action extraction.
- Added frozen dictionary usage in relevant hot lookups while preserving compatibility fallbacks.

### Notes
- No poll renderer or explicit creator-goal action payload was observed in the analyzed `log7`/`log8` action stream. Goal/points entity keys are present in framework update/UI state metadata, but not yet mapped as first-class chat events.
- Continuous livestream monitor mode is currently BETA/UNSUPPORTED. It may change or break at any time and is intentionally not covered by semver stability guarantees until promoted from beta.
