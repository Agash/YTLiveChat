using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

/// <summary>
/// View-compare mode: reads one live stream's chat twice at the same time through the library, once in each
/// <see cref="ChatView"/>, and reports how many messages each view received and what only one of them had.
/// </summary>
internal static class ViewCompareMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        int durationSeconds = 120;
        string? liveId = null;
        foreach (string arg in args)
        {
            if (arg.StartsWith("--duration=", StringComparison.Ordinal))
            {
                durationSeconds = int.Parse(
                    arg["--duration=".Length..],
                    CultureInfo.InvariantCulture
                );
            }
            else
            {
                liveId = arg;
            }
        }

        if (liveId is null)
        {
            Console.WriteLine("Usage: viewcompare [--duration=s] <videoId>");
            return 1;
        }

        ConcurrentDictionary<string, string> all = new(StringComparer.Ordinal);
        ConcurrentDictionary<string, string> top = new(StringComparer.Ordinal);
        using HttpClient allHttp = new() { BaseAddress = new Uri("https://www.youtube.com") };
        using HttpClient topHttp = new() { BaseAddress = new Uri("https://www.youtube.com") };
        using YTLiveChat.Services.YTLiveChat allChat = Session(allHttp, ChatView.AllMessages, all);
        using YTLiveChat.Services.YTLiveChat topChat = Session(topHttp, ChatView.TopChat, top);

        allChat.Start(liveId: liveId);
        topChat.Start(liveId: liveId);
        await Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ConfigureAwait(false);
        allChat.Stop();
        topChat.Stop();

        string[] onlyAll = [.. all.Keys.Except(top.Keys)];
        string[] onlyTop = [.. top.Keys.Except(all.Keys)];
        Console.WriteLine(
            $"{liveId}: all messages {all.Count}, top chat {top.Count}, only in all {onlyAll.Length}, only in top {onlyTop.Length}"
        );
        foreach (string id in onlyAll.Take(10))
        {
            Console.WriteLine($"  only in all: {all[id]}");
        }

        return 0;
    }

    private static YTLiveChat.Services.YTLiveChat Session(
        HttpClient http,
        ChatView view,
        ConcurrentDictionary<string, string> received
    )
    {
        YTLiveChat.Services.YTLiveChat chat = new(
            new YTLiveChatOptions { ChatView = view, RequestFrequency = 1000 },
            new ContinuationRecordingClient(http, view)
        );
        chat.ChatReceived += (_, e) =>
            received[e.ChatItem.Id] =
                $"{e.ChatItem.Author.Name}: {string.Concat(e.ChatItem.Message.Select(static p => p.ToString()))}"[
                    ..Math.Min(
                        80,
                        $"{e.ChatItem.Author.Name}: {string.Concat(e.ChatItem.Message.Select(static p => p.ToString()))}".Length
                    )
                ];
        return chat;
    }
}

/// <summary>Prints the chat type each of the first requested continuations carries (field 16.1: 4 Top chat, 1 all).</summary>
internal sealed class ContinuationRecordingClient(HttpClient http, ChatView view)
    : YTHttpClient(http, NullLogger<YTHttpClient>.Instance)
{
    private int _requests;

    public override Task<(
        YTLiveChat.Models.Response.LiveChatResponse? Response,
        string? RawJson
    )> GetLiveChatAsync(
        YTLiveChat.Models.FetchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (Interlocked.Increment(ref _requests) <= 3)
        {
            Console.WriteLine(
                $"  {view}: request {_requests} chat type {ChatTypeOf(options.Continuation)}"
            );
        }

        return base.GetLiveChatAsync(options, cancellationToken);
    }

    // The continuation is a protobuf whose field 16 holds the chat type in its field 1.
    private static string ChatTypeOf(string continuation)
    {
        try
        {
            string padded = Uri.UnescapeDataString(continuation)
                .Replace('-', '+')
                .Replace('_', '/');
            padded += new string('=', (4 - (padded.Length % 4)) % 4);
            byte[] bytes = Convert.FromBase64String(padded);
            return FindChatType(bytes, 0, bytes.Length)?.ToString(CultureInfo.InvariantCulture)
                ?? "?";
        }
        catch (FormatException)
        {
            return "?";
        }
    }

    private static long? FindChatType(byte[] buffer, int start, int end)
    {
        int i = start;
        while (i < end)
        {
            long key = ReadVarint(buffer, ref i);
            int field = (int)(key >> 3);
            int wire = (int)(key & 7);
            if (wire == 0)
            {
                _ = ReadVarint(buffer, ref i);
            }
            else if (wire == 2)
            {
                int length = (int)ReadVarint(buffer, ref i);
                if (field == 16)
                {
                    int j = i;
                    while (j < i + length)
                    {
                        long innerKey = ReadVarint(buffer, ref j);
                        long value = ReadVarint(buffer, ref j);
                        if (innerKey >> 3 == 1)
                        {
                            return value;
                        }
                    }
                }
                else if (FindChatType(buffer, i, i + length) is { } nested)
                {
                    return nested;
                }

                i += length;
            }
            else if (wire == 5)
            {
                i += 4;
            }
            else if (wire == 1)
            {
                i += 8;
            }
            else
            {
                return null;
            }
        }

        return null;
    }

    private static long ReadVarint(byte[] buffer, ref int i)
    {
        long value = 0;
        int shift = 0;
        while (i < buffer.Length)
        {
            byte b = buffer[i++];
            value |= (long)(b & 0x7F) << shift;
            if (b < 0x80)
            {
                return value;
            }

            shift += 7;
        }

        throw new FormatException();
    }
}
