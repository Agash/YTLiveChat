using System.Text.RegularExpressions;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Helpers
{
    internal static partial class Parser
    {
        public static FetchOptions GetOptionsFromLivePage(string raw)
        {
            Match idResult = LiveIdRegex().Match(raw);
            string liveId = idResult.Success ? idResult.Groups[1].Value : throw new Exception("Live Stream was not found");

            Match replayResult = ReplayRegex().Match(raw);
            if (replayResult.Success)
            {
                throw new Exception($"{liveId} is finished live");
            }

            Match keyResult = ApiKeyRegex().Match(raw);
            string apiKey = keyResult.Success ? keyResult.Groups[1].Value : throw new Exception("API Key was not found");

            Match verResult = ClientVersionRegex().Match(raw);
            string clientVersion = verResult.Success ? verResult.Groups[1].Value : throw new Exception("Client Version was not found");

            Match continuationResult = ContinuationRegex().Match(raw);
            string continuation = continuationResult.Success ? continuationResult.Groups[1].Value : throw new Exception("Continuation was not found");

            return new()
            {
                ApiKey = apiKey,
                ClientVersion = clientVersion,
                Continuation = continuation,
                LiveId = liveId
            };
        }
        public static MessageRendererBase? GetMessageRenderer(AddChatItemAction.ItemObj item)
        {
            return item == null
                ? null
                : item.LiveChatPaidMessageRenderer ?? item.LiveChatTextMessageRenderer ?? item.LiveChatPaidStickerRenderer ?? item.LiveChatMembershipItemRenderer ?? (MessageRendererBase?)null;
        }
        public static MessagePart ToMessagePart(this MessageRun run)
        {
            if (run is MessageText text)
            {
                return new TextPart { Text = text.Text };
            }

            MessageEmoji? emoji = run as MessageEmoji;

            bool isCustom = emoji?.Emoji.IsCustomEmoji ?? false;
            string? altText = emoji?.Emoji.Shortcuts.FirstOrDefault();

            return new EmojiPart
            {
                Url = emoji?.Emoji.Image?.Thumbnails.FirstOrDefault()?.Url ?? string.Empty,
                IsCustomEmoji = isCustom,
                Alt = altText,
                EmojiText = (isCustom ? altText : emoji?.Emoji.EmojiId) ?? string.Empty
            };
        }

        public static MessagePart[] ToMessagePart(this MessageRun[] runs)
        {
            return runs.Select(r => r.ToMessagePart()).ToArray();
        }

        public static ChatItem? ToChatItem(this YTAction action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (action.AddChatItemAction == null)
            {
                return null;
            }

            MessageRendererBase? renderer = GetMessageRenderer(action.AddChatItemAction.Item);
            if (renderer == null)
            {
                return null;
            }

            ChatItem chat = new()
            {
                Id = renderer.Id,
                Author = new()
                {
                    Name = renderer.AuthorName?.SimpleText ?? string.Empty,
                    ChannelId = renderer.AuthorExternalChannelId,
                    Thumbnail = renderer.AuthorPhoto.Thumbnails.ToImage(renderer.AuthorName?.SimpleText ?? string.Empty)
                },
                Message = renderer switch
                {
                    LiveChatTextMessageRenderer textMessageRenderer => textMessageRenderer.Message.Runs.ToMessagePart(),
                    LiveChatMembershipItemRenderer membershipItemRenderer => membershipItemRenderer.HeaderSubtext.Runs.ToMessagePart(),
                    _ => []
                },
                Superchat = renderer switch
                {
                    LiveChatPaidStickerRenderer stickerRenderer => new()
                    {
                        Amount = stickerRenderer.PurchaseAmountText.SimpleText ?? string.Empty,
                        Color = stickerRenderer.BackgroundColor.ToHex6Color(),
                        Sticker = stickerRenderer.Sticker.Thumbnails.ToImage(stickerRenderer.Sticker.Accessibility.AccessibilityData.Label)
                    },
                    LiveChatPaidMessageRenderer paidMessageRenderer => new()
                    {
                        Amount = paidMessageRenderer.PurchaseAmountText.SimpleText ?? string.Empty,
                        Color = paidMessageRenderer.BodyBackgroundColor.ToHex6Color(),
                    },
                    _ => null
                }
            };

            if (renderer.AuthorBadges != null && renderer.AuthorBadges.Length > 0)
            {
                foreach (AuthorBadge item in renderer.AuthorBadges)
                {
                    AuthorBadgeRenderer badge = item.LiveChatAuthorBadgeRenderer;
                    if (badge.CustomThumbnail != null)
                    {
                        chat.Author.Badge = new Badge
                        {
                            Thumbnail = badge.CustomThumbnail.Thumbnails.ToImage(badge.Tooltip),
                            Label = badge.Tooltip
                        };
                        chat.IsMembership = true;
                    }
                    else
                    {
                        switch (badge.Icon?.IconType)
                        {
                            case "OWNER":
                                chat.IsOwner = true; break;
                            case "VERIFIED":
                                chat.IsVerified = true; break;
                            case "MODERATOR":
                                chat.IsModerator = true; break;
                        }
                    }
                }
            }

            return chat;
        }
        public static ImagePart? ToImage(this Thumbnail[] thumbnails, string? alt = null)
        {
            Thumbnail? thumbnail = thumbnails.LastOrDefault();
            return thumbnail == null
                ? null
                : new ImagePart
                {
                    Url = thumbnail.Url,
                    Alt = alt,
                };
        }
        public static (List<ChatItem> Items, string Continuation) ParseGetLiveChatResponse(GetLiveChatResponse? response)
        {
            List<ChatItem> items = [];

            if (response != null)
            {
                items = response.ContinuationContents.LiveChatContinuation.Actions.Where(a => a.AddChatItemAction != null).Select(a => a.ToChatItem()).WhereNotNull().ToList();
            }

            Continuation? continuationData = response?.ContinuationContents.LiveChatContinuation.Continuations.FirstOrDefault();
            string continuation = "";

            if (continuationData?.InvalidationContinuationData != null)
            {
                continuation = continuationData.InvalidationContinuationData.Continuation;
            }
            else if (continuationData?.TimedContinuationData != null)
            {
                continuation = continuationData.TimedContinuationData.Continuation;
            }

            return (items, continuation);
        }

        [GeneratedRegex("<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">")]
        private static partial Regex LiveIdRegex();

        [GeneratedRegex("\"isReplay\":\\s*(true)")]
        private static partial Regex ReplayRegex();

        [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"[^\"]*\"")]
        private static partial Regex ApiKeyRegex();

        [GeneratedRegex("\"clientVersion\":\\s*\"[^\"]*\"")]
        private static partial Regex ClientVersionRegex();

        [GeneratedRegex("\"continuation\":\\s*\"[^\"]*\"")]
        private static partial Regex ContinuationRegex();
    }
}
