// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Reasoning: These features are not available in .netstandard2.1 or 2.1 without Polyfill

[assembly: SuppressMessage(
    "Maintainability",
    "CA1510:Use ArgumentNullException throw helper",
    Justification = "Not available in netstandard2.0+",
    Scope = "member",
    Target = "~M:YTLiveChat.Services.YTLiveChat.#ctor(YTLiveChat.Contracts.YTLiveChatOptions,YTLiveChat.Services.YTHttpClient,Microsoft.Extensions.Logging.ILogger{YTLiveChat.Services.YTLiveChat})"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1510:Use ArgumentNullException throw helper",
    Justification = "Not available in netstandard2.0+",
    Scope = "member",
    Target = "~M:YTLiveChat.Helpers.Parser.ToChatItem(YTLiveChat.Models.Response.Action)~YTLiveChat.Contracts.Models.ChatItem"
)]
[assembly: SuppressMessage(
    "Usage",
    "CA2249:Consider using 'string.Contains' instead of 'string.IndexOf'",
    Justification = "Not available in netstandard2.0+",
    Scope = "member",
    Target = "~M:YTLiveChat.Helpers.Parser.ToChatItem(YTLiveChat.Models.Response.Action)~YTLiveChat.Contracts.Models.ChatItem"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1866:Use char overload",
    Justification = "Not available in netstandard2.0+",
    Scope = "member",
    Target = "~M:YTLiveChat.Services.YTHttpClient.GetOptionsAsync(System.String,System.String,System.String,System.Threading.CancellationToken)~System.Threading.Tasks.Task{System.String}"
)]
