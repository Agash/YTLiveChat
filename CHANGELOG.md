### Added (formatting API)
- `TextPart` now carries `Bold`, `Italics`, and `IsDeemphasized` flags populated via `ToMessagePart()`.
  `ToMessagePart()` is the single point of change for rich-text rendering support going forward.
- `EngagementItem.Message` is `MessagePart[]` (consistent with `ChatItem.Message`).
- `PollChoice.Text` is `MessagePart[]`. Concatenate `TextPart.Text` values for a plain-text label.
- `PollItem.Question` is `MessagePart[]?`. Concatenate `TextPart.Text` values for a plain-text label.
- `ChatSummaryBannerItem.Summary` is `MessagePart[]`, preserving all runs (bold title, deemphasized
  disclaimer, body text) from the `chatSummary.runs` array.
- `liveChatViewerEngagementMessageRenderer` is deserialized into a typed
  `LiveChatViewerEngagementMessageRenderer` record, enabling proper `Bold`/`Italics`/`IsDeemphasized`
  pass-through for poll result and other engagement messages.

### Added
- `MembershipEventType.Upgraded` — tier-upgrade membership events; detected when `headerSubtext`
  uses the runs shape `["Upgraded membership to ", "{TierName}", "!"]`. Tier name is extracted from
  the second run (same mechanism as `New` events). Confirmed against a real InnerTube capture.

### Added (recent)
- `Author.ChannelHandle` — the author's `@handle`, populated for ticker bar items where YouTube includes it directly in the outer ticker renderer (`liveChatTickerPaidMessageItemRenderer.authorUsername`). Parser also falls back to `authorPhoto.accessibility.accessibilityData.label` which carries the same value on all observed ticker paid-message items.
- `ChatItem.ViewerLeaderboardRank` — leaderboard rank extracted from `leaderboardBadge.buttonViewModel.title` (e.g. `"#1"`) on paid messages. Previously unmodeled.
- `ThumbnailList.Accessibility` (internal response model) — the optional `accessibility` container present on `authorPhoto` objects in ticker outer item renderers; `accessibilityData.label` carries the author's `@handle` as a secondary source.
- `LiveChatMembershipItemRenderer.Empty` (internal response model) — boolean flag YouTube sets `true` (~3.7% of observed instances) on milestone membership items that carry no message body. Parser produces an empty `Message` array for these naturally.
- Expanded ticker outer item renderer models with all observed fields:
  - `LiveChatTickerPaidMessageItemRenderer`: `AuthorExternalChannelId`, `AuthorPhoto`, `AuthorUsername`, `StartBackgroundColor`, `EndBackgroundColor`, `AmountTextColor`, `DurationSec`, `FullDurationSec`, `TrackingParams`.
  - `LiveChatTickerSponsorItemRenderer`: `AuthorExternalChannelId`, `SponsorPhoto`, `DetailText`, `DetailTextColor`, `DetailIcon`, `StartBackgroundColor`, `EndBackgroundColor`, `DurationSec`, `FullDurationSec`, `TrackingParams`.
  - `LiveChatTickerPaidStickerItemRenderer`: `AuthorExternalChannelId`, `AuthorPhoto`, `TickerThumbnails`, `StartBackgroundColor`, `EndBackgroundColor`, `DurationSec`, `FullDurationSec`, `TrackingParams`.
- Parser now back-fills `Author.ChannelId` and `Author.Thumbnail` from the outer ticker item renderer when the nested `showLiveChatItemEndpoint` renderer omits them.
- Added `watch` subcommand to `YTLiveChat.Tools` for live capture of unknown and unrecognized events to a JSONL file.
- Added `analyze` subcommand to `YTLiveChat.Tools` for field-level baseline diffing against the C# response model.

### Renamed (internal response models)
- `AuthorPhoto` response record renamed to `ThumbnailList` — the `{ thumbnails: [...] }` container structure is reused for author photos, sticker images, poll header thumbnails, and ticker bar thumbnails; the new name reflects this generic role. Only affects `YTLiveChat.Models.Response` (internal namespace).

### Breaking Changes
- `MembershipDetails.LevelName` is now treated as membership tier information when available (for example from welcome subtext runs), instead of reflecting badge tenure labels.
- New-member membership events now preserve welcome message runs in `ChatItem.Message` (`Welcome to {tier}!`) where available. Consumers previously assuming empty message arrays for new-member events must adjust.
- IMPORTANT: See ChatItem.IsTicker information below.

### Added
- Added raw action surface: `IYTLiveChat.RawActionReceived` with `RawActionReceivedEventArgs.RawAction` and optional `ParsedChatItem` mapping.
- Added `ChatItem.ViewerLeaderboardRank` extraction for YouTube points leaderboard rank tags (for example `#1`, `#2`, `#3`).
- Added `MembershipDetails.MembershipBadgeLabel` to preserve badge/tenure text separately from tier name.
- Added `ChatItem.IsTicker` to indicate events sourced from `addLiveChatTickerItemAction`.
- Added ticker event parsing support:
  - `liveChatTickerPaidMessageItemRenderer` -> parsed super chat item.
  - `liveChatTickerSponsorItemRenderer` -> parsed membership item.
  IMPORTANT: These appear twice, once as ticker and once as message if captured while the event happens, disregard ChatItem.IsTicker=true values to dedupe if counting/collecting donations.
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


### Notes
- Continuous livestream monitor mode is currently BETA/UNSUPPORTED. It may change or break at any time and is intentionally not covered by semver stability guarantees until promoted from beta.
