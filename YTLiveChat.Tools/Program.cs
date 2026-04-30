using System.Text;
using System.Text.Json;

if (args.Length > 0 && args[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
{
    return await WatchMode.RunAsync(args[1..]);
}

if (args.Length > 0 && args[0].Equals("snapshot", StringComparison.OrdinalIgnoreCase))
{
    return await SnapshotMode.RunAsync(args[1..]);
}

if (args.Length > 0 && args[0].Equals("analyze", StringComparison.OrdinalIgnoreCase))
{
    return AnalyzeMode.Run(args[1..]);
}

// "count" is the explicit name for the default dump/count mode; also accepted without a subcommand.
string[] countArgs = args.Length > 0 && args[0].Equals("count", StringComparison.OrdinalIgnoreCase)
    ? args[1..]
    : args;

Options options = ParseOptions(countArgs);
if (options.Paths.Count == 0)
{
    options = PromptOptionsInteractive();
    if (options.Paths.Count == 0)
    {
        PrintUsage();
        return 1;
    }
}


// Dump mode: collect full untruncated JSON for targeted extraction
List<JsonElement> dumpedRenderers = [];
List<JsonElement> dumpedActions = [];

// Parsed: produce ChatItems (ChatReceived)
HashSet<string> chatItemActionTypes =
[
    "addChatItemAction",
    "addLiveChatTickerItemAction",
];

// Parsed: fire a dedicated public event (not a ChatItem)
HashSet<string> parsedDedicatedEventActionTypes =
[
    "removeChatItemAction",                  // → ChatItemDeleted
    "replaceChatItemAction",                 // → ChatItemReplaced
    "removeChatItemByAuthorAction",          // → ChatItemsDeletedByAuthor
    "markChatItemsByAuthorAsDeletedAction",  // → ChatItemsDeletedByAuthor
    "changeEngagementPanelVisibilityAction", // → EngagementMessageReceived
    // Poll lifecycle
    "showLiveChatActionPanelAction",         // → PollUpdated (new poll)
    "updateLiveChatPollAction",              // → PollUpdated (vote update)
    "closeLiveChatActionPanelAction",        // → PollClosed
    // Banner lifecycle
    "addBannerToLiveChatCommand",            // → BannerAdded
    "removeBannerForLiveChatCommand",        // → BannerRemoved
];

// Known but intentionally silent: recognized by the library, no public event emitted
HashSet<string> silentActionTypes =
[
    "signalAction",
    "liveChatReportModerationStateCommand",
    // Fanzone ticker chip — members-only event UI chip, no parseable data content
    "showFanzoneTickerChipCommand",
    "removeFanzoneTickerChipCommand",
];

// Combined set for "is this action known at all?" checks
HashSet<string> knownActionTypes =
[
    .. chatItemActionTypes,
    .. parsedDedicatedEventActionTypes,
    .. silentActionTypes,
];

HashSet<string> knownItemRenderers =
[
    "liveChatTextMessageRenderer",
    "liveChatPaidMessageRenderer",
    "liveChatPaidStickerRenderer",
    "liveChatMembershipItemRenderer",
    "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer",
    "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer",
    "liveChatPlaceholderItemRenderer",
    // System/engagement messages — no public event, treated as informational noise
    "liveChatViewerEngagementMessageRenderer",
    "liveChatModeChangeMessageRenderer",
    // YouTube Jewels virtual gift — fires GiftReceived dedicated event, not ChatItem
    "giftMessageViewModel",
];

HashSet<string> membershipRenderers =
[
    "liveChatMembershipItemRenderer",
    "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer",
    "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer",
];

Dictionary<string, int> actionCounts = new(StringComparer.Ordinal);
Dictionary<string, int> rendererCounts = new(StringComparer.Ordinal);
Dictionary<string, int> tickerRendererCounts = new(StringComparer.Ordinal);
Dictionary<string, int> nestedRendererCounts = new(StringComparer.Ordinal);
Dictionary<string, int> unknownRendererCounts = new(StringComparer.Ordinal);
List<string> parseErrors = [];

Dictionary<string, int> membershipVariants = new(StringComparer.Ordinal);
Dictionary<string, int> superChatVariants = new(StringComparer.Ordinal);
Dictionary<string, int> superStickerVariants = new(StringComparer.Ordinal);
Dictionary<string, int> unknownRendererVariants = new(StringComparer.Ordinal);
Dictionary<string, string> variantSamples = new(StringComparer.Ordinal);

foreach (string path in options.Paths)
{
    if (!File.Exists(path))
    {
        parseErrors.Add($"Missing file: {path}");
        continue;
    }

    try
    {
        foreach (JsonElement action in LogReader.ReadActions(path))
        {
            string? actionType = LogReader.GetActionType(action);
            if (actionType == null)
            {
                // No action/command property — tracking-only entry (e.g. only clickTrackingParams).
                // These carry no event data and are not counted as unknown actions.
                Increment(actionCounts, "<tracking-only>");
                continue;
            }

            Increment(actionCounts, actionType);
            TryDumpAction(actionType, action, options, dumpedActions);

            if (
                actionType == "addChatItemAction"
                && action.TryGetProperty("addChatItemAction", out JsonElement addChat)
                && addChat.TryGetProperty("item", out JsonElement item)
                && item.ValueKind == JsonValueKind.Object
                && LogReader.TryGetSingleRenderer(item, out string? rendererType, out JsonElement rendererValue)
            )
            {
                Increment(rendererCounts, rendererType!);
                AnalyzeRenderer(
                    rendererType!,
                    rendererValue,
                    "addChatItemAction",
                    knownItemRenderers,
                    membershipRenderers,
                    options,
                    unknownRendererCounts,
                    membershipVariants,
                    superChatVariants,
                    superStickerVariants,
                    unknownRendererVariants,
                    variantSamples
                );
                TryDumpRenderer(rendererType!, rendererValue, options, dumpedRenderers);
            }
            else if (
                actionType == "addLiveChatTickerItemAction"
                && action.TryGetProperty("addLiveChatTickerItemAction", out JsonElement tickerAction)
                && tickerAction.TryGetProperty("item", out JsonElement tickerItem)
                && tickerItem.ValueKind == JsonValueKind.Object
            )
            {
                if (LogReader.TryGetSingleRenderer(tickerItem, out string? tickerRenderer, out JsonElement tickerOuterValue))
                {
                    Increment(tickerRendererCounts, tickerRenderer!);
                    TryDumpRenderer(tickerRenderer!, tickerOuterValue, options, dumpedRenderers);
                }

                if (
                    LogReader.TryGetNestedShowRenderer(
                        tickerItem,
                        out string? nestedRenderer,
                        out JsonElement nestedRendererValue
                    )
                )
                {
                    Increment(nestedRendererCounts, nestedRenderer!);
                    AnalyzeRenderer(
                        nestedRenderer!,
                        nestedRendererValue,
                        "ticker.showLiveChatItemEndpoint",
                        knownItemRenderers,
                        membershipRenderers,
                        options,
                        unknownRendererCounts,
                        membershipVariants,
                        superChatVariants,
                        superStickerVariants,
                        unknownRendererVariants,
                        variantSamples
                    );
                    TryDumpRenderer(nestedRenderer!, nestedRendererValue, options, dumpedRenderers);
                }
            }
        }
    }
    catch (Exception ex)
    {
        parseErrors.Add($"{path}: {ex.Message}");
    }
}

Console.WriteLine("== Action Types ==");
PrintSorted(actionCounts);

Console.WriteLine();
Console.WriteLine("== addChatItemAction Renderers ==");
PrintSorted(rendererCounts);

Console.WriteLine();
Console.WriteLine("== addLiveChatTickerItemAction Item Renderers ==");
PrintSorted(tickerRendererCounts);

Console.WriteLine();
Console.WriteLine("== Ticker Nested showLiveChatItemEndpoint Renderers ==");
PrintSorted(nestedRendererCounts);

Console.WriteLine();
Console.WriteLine("== Parsed Actions (dedicated event, no ChatItem) ==");
foreach (
    KeyValuePair<string, int> kv in actionCounts
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key, StringComparer.Ordinal)
)
{
    if (parsedDedicatedEventActionTypes.Contains(kv.Key))
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
    }
}

Console.WriteLine();
Console.WriteLine("== Silent Actions (recognized, no public event) ==");
foreach (
    KeyValuePair<string, int> kv in actionCounts
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key, StringComparer.Ordinal)
)
{
    if (silentActionTypes.Contains(kv.Key))
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
    }
}

Console.WriteLine();
Console.WriteLine("== Unknown Actions (not in known parser surface) ==");
foreach (
    KeyValuePair<string, int> kv in actionCounts
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key, StringComparer.Ordinal)
)
{
    if (!knownActionTypes.Contains(kv.Key) && kv.Key != "<tracking-only>")
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
    }
}

Console.WriteLine();
Console.WriteLine("== Potentially Unsupported Item Renderers ==");
PrintSorted(unknownRendererCounts);

if (options.EnableVariants)
{
    Console.WriteLine();
    Console.WriteLine("== Membership Variants ==");
    PrintVariants(membershipVariants, variantSamples, options.MaxVariantRows);

    Console.WriteLine();
    Console.WriteLine("== Super Chat Variants ==");
    PrintVariants(superChatVariants, variantSamples, options.MaxVariantRows);

    Console.WriteLine();
    Console.WriteLine("== Super Sticker Variants ==");
    PrintVariants(superStickerVariants, variantSamples, options.MaxVariantRows);

    Console.WriteLine();
    Console.WriteLine("== Unknown Renderer Variants ==");
    PrintVariants(unknownRendererVariants, variantSamples, options.MaxVariantRows);
}

if (options.DumpRenderer != null)
{
    Console.WriteLine();
    Console.WriteLine($"== Dump Renderer: {options.DumpRenderer}{(options.FilterSubtext != null ? $" (headerSubtext prefix: \"{options.FilterSubtext}\")" : "")} ==");
    Console.WriteLine($"  {dumpedRenderers.Count} match(es)");

    JsonSerializerOptions prettyJson = new() { WriteIndented = true };
    string dumpJson = JsonSerializer.Serialize(dumpedRenderers, prettyJson);

    if (options.DumpOutput != null)
    {
        File.WriteAllText(options.DumpOutput, dumpJson, Encoding.UTF8);
        Console.WriteLine($"  Written to: {options.DumpOutput}");
    }
    else
    {
        Console.WriteLine(dumpJson);
    }
}

if (options.DumpAction != null)
{
    Console.WriteLine();
    Console.WriteLine($"== Dump Action: {options.DumpAction} ==");
    Console.WriteLine($"  {dumpedActions.Count} match(es)");

    JsonSerializerOptions prettyJson = new() { WriteIndented = true };
    string dumpJson = JsonSerializer.Serialize(dumpedActions, prettyJson);

    if (options.DumpOutput != null)
    {
        File.WriteAllText(options.DumpOutput, dumpJson, Encoding.UTF8);
        Console.WriteLine($"  Written to: {options.DumpOutput}");
    }
    else
    {
        Console.WriteLine(dumpJson);
    }
}

if (parseErrors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("== Parse Errors ==");
    foreach (string error in parseErrors)
    {
        Console.WriteLine(error);
    }

    return 2;
}

return 0;

static void TryDumpAction(
    string actionType,
    JsonElement action,
    Options options,
    List<JsonElement> dumpedActions
)
{
    if (options.DumpAction == null)
    {
        return;
    }

    if (!actionType.Equals(options.DumpAction, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    if (options.DumpLimit.HasValue && dumpedActions.Count >= options.DumpLimit.Value)
    {
        return;
    }

    if (options.FilterHasField != null && !HasFieldAnywhere(action, options.FilterHasField))
    {
        return;
    }

    dumpedActions.Add(action.Clone());
}

static void TryDumpRenderer(
    string rendererType,
    JsonElement rendererValue,
    Options options,
    List<JsonElement> dumpedRenderers
)
{
    if (options.DumpRenderer == null)
    {
        return;
    }

    if (!rendererType.Equals(options.DumpRenderer, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    if (options.DumpLimit.HasValue && dumpedRenderers.Count >= options.DumpLimit.Value)
    {
        return;
    }

    if (options.FilterSubtext != null)
    {
        string? headerSubtext = LogReader.TryGetSimpleText(rendererValue, "headerSubtext")
            ?? LogReader.TryGetRunsAsPlainText(rendererValue, "headerSubtext")
            ?? LogReader.TryGetSimpleText(rendererValue, "headerPrimaryText")
            ?? LogReader.TryGetRunsAsPlainText(rendererValue, "headerPrimaryText");

        if (
            headerSubtext == null
            || !headerSubtext.StartsWith(options.FilterSubtext, StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }
    }

    if (options.FilterHasField != null && !HasFieldAnywhere(rendererValue, options.FilterHasField))
    {
        return;
    }

    dumpedRenderers.Add(rendererValue.Clone());
}

/// <summary>
/// Returns true if <paramref name="element"/> contains a property named
/// <paramref name="fieldName"/> at any nesting depth.
/// </summary>
static bool HasFieldAnywhere(JsonElement element, string fieldName)
{
    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(fieldName, StringComparison.Ordinal))
            {
                return true;
            }

            if (HasFieldAnywhere(property.Value, fieldName))
            {
                return true;
            }
        }
    }
    else if (element.ValueKind == JsonValueKind.Array)
    {
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (HasFieldAnywhere(item, fieldName))
            {
                return true;
            }
        }
    }

    return false;
}

static void AnalyzeRenderer(
    string rendererType,
    JsonElement rendererValue,
    string source,
    HashSet<string> knownItemRenderers,
    HashSet<string> membershipRenderers,
    Options options,
    Dictionary<string, int> unknownRendererCounts,
    Dictionary<string, int> membershipVariants,
    Dictionary<string, int> superChatVariants,
    Dictionary<string, int> superStickerVariants,
    Dictionary<string, int> unknownRendererVariants,
    Dictionary<string, string> variantSamples
)
{
    if (!knownItemRenderers.Contains(rendererType))
    {
        Increment(unknownRendererCounts, rendererType);
    }

    if (!options.EnableVariants)
    {
        return;
    }

    string variantSignature = BuildVariantSignature(rendererType, rendererValue, source);

    if (membershipRenderers.Contains(rendererType))
    {
        RecordVariant(membershipVariants, variantSamples, variantSignature, rendererValue);
        return;
    }

    if (rendererType == "liveChatPaidMessageRenderer")
    {
        RecordVariant(superChatVariants, variantSamples, variantSignature, rendererValue);
        return;
    }

    if (rendererType == "liveChatPaidStickerRenderer")
    {
        RecordVariant(superStickerVariants, variantSamples, variantSignature, rendererValue);
        return;
    }

    if (!knownItemRenderers.Contains(rendererType))
    {
        RecordVariant(unknownRendererVariants, variantSamples, variantSignature, rendererValue);
    }
}

static Options ParseOptions(string[] args)
{
    List<string> paths = [];
    bool enableVariants = false;
    int maxVariantRows = 25;
    string? dumpRenderer = null;
    string? dumpAction = null;
    string? filterSubtext = null;
    string? filterHasField = null;
    int? dumpLimit = null;
    string? dumpOutput = null;

    foreach (string arg in args)
    {
        if (arg.Equals("--variants", StringComparison.OrdinalIgnoreCase))
        {
            enableVariants = true;
            continue;
        }

        const string maxRowsPrefix = "--max-variant-rows=";
        if (arg.StartsWith(maxRowsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string rawValue = arg[maxRowsPrefix.Length..];
            if (int.TryParse(rawValue, out int parsed) && parsed > 0)
            {
                maxVariantRows = parsed;
            }

            continue;
        }

        const string dumpRendererPrefix = "--dump-renderer=";
        if (arg.StartsWith(dumpRendererPrefix, StringComparison.OrdinalIgnoreCase))
        {
            dumpRenderer = arg[dumpRendererPrefix.Length..];
            continue;
        }

        const string dumpActionPrefix = "--dump-action=";
        if (arg.StartsWith(dumpActionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            dumpAction = arg[dumpActionPrefix.Length..];
            continue;
        }

        const string filterSubtextPrefix = "--filter-subtext=";
        if (arg.StartsWith(filterSubtextPrefix, StringComparison.OrdinalIgnoreCase))
        {
            filterSubtext = arg[filterSubtextPrefix.Length..];
            continue;
        }

        const string filterHasFieldPrefix = "--filter-has-field=";
        if (arg.StartsWith(filterHasFieldPrefix, StringComparison.OrdinalIgnoreCase))
        {
            filterHasField = arg[filterHasFieldPrefix.Length..];
            continue;
        }

        const string limitPrefix = "--limit=";
        if (arg.StartsWith(limitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string rawValue = arg[limitPrefix.Length..];
            if (int.TryParse(rawValue, out int parsed) && parsed > 0)
            {
                dumpLimit = parsed;
            }

            continue;
        }

        const string dumpOutputPrefix = "--dump-output=";
        if (arg.StartsWith(dumpOutputPrefix, StringComparison.OrdinalIgnoreCase))
        {
            dumpOutput = arg[dumpOutputPrefix.Length..];
            continue;
        }

        if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            Environment.Exit(0);
        }

        paths.Add(arg);
    }

    // Expand any directory paths to *.jsonl files within them
    List<string> expandedPaths = [.. LogReader.ExpandPaths(paths)];
    return new(expandedPaths, enableVariants, maxVariantRows, dumpRenderer, dumpAction, filterSubtext, filterHasField, dumpLimit, dumpOutput);
}

static Options PromptOptionsInteractive()
{
    Console.WriteLine("No log paths were passed via args. Switching to interactive mode.");
    Console.WriteLine("Enter one or more log paths or directories separated by ';' or ',':");
    Console.Write("> ");
    string? pathsInput = Console.ReadLine();

    List<string> paths = [];
    if (!string.IsNullOrWhiteSpace(pathsInput))
    {
        foreach (
            string segment in pathsInput.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
        )
        {
            string path = segment.Trim();
            if (!string.IsNullOrWhiteSpace(path))
            {
                paths.Add(path);
            }
        }
    }

    Console.WriteLine("Enable variant analysis? (Y/n)");
    Console.Write("> ");
    string? variantInput = Console.ReadLine();
    bool enableVariants = string.IsNullOrWhiteSpace(variantInput)
        || variantInput.Equals("y", StringComparison.OrdinalIgnoreCase)
        || variantInput.Equals("yes", StringComparison.OrdinalIgnoreCase);

    int maxVariantRows = 25;
    Console.WriteLine("Max variant rows to print per section [default: 25]:");
    Console.Write("> ");
    string? rowsInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(rowsInput) && int.TryParse(rowsInput, out int parsed) && parsed > 0)
    {
        maxVariantRows = parsed;
    }

    List<string> expandedInteractive = [.. LogReader.ExpandPaths(paths)];
    return new(expandedInteractive, enableVariants, maxVariantRows, DumpRenderer: null, DumpAction: null, FilterSubtext: null, FilterHasField: null, DumpLimit: null, DumpOutput: null);
}

static void PrintUsage()
{
    Console.WriteLine("YTLiveChat.Tools — log analysis and live capture for InnerTube chat data");
    Console.WriteLine();
    Console.WriteLine("Subcommands:");
    Console.WriteLine("  watch     Capture live chat from one or more streams to .jsonl files.");
    Console.WriteLine("  snapshot  Fetch and save YouTube page HTML snapshots for test fixtures.");
    Console.WriteLine("  count     Count and dump renderer/action types from captured .jsonl logs. (default)");
    Console.WriteLine("  analyze   Field-level baseline diff + deep recursive scan for new/unknown JSON keys.");
    Console.WriteLine();
    Console.WriteLine("━━━ watch ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- watch <@handle|UCxxx|liveId> [...]");
    Console.WriteLine();
    Console.WriteLine("  Watches one or more live streams and appends every raw action JSON to a");
    Console.WriteLine("  timestamped .jsonl file in the current directory.");
    Console.WriteLine();
    Console.WriteLine("━━━ count ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- count [options] <logPath|dir> [...]");
    Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- [options] <logPath|dir> [...]  (shorthand)");
    Console.WriteLine();
    Console.WriteLine("  Options:");
    Console.WriteLine("    --variants                  Variant signatures for memberships, super chats/stickers, unknown renderers.");
    Console.WriteLine("    --max-variant-rows=<n>      Max rows per variant section. Default: 25.");
    Console.WriteLine("    --dump-renderer=<name>      Extract full JSON for all matching addChatItemAction renderer types.");
    Console.WriteLine("    --dump-action=<name>        Extract full JSON for all matching top-level action types.");
    Console.WriteLine("                                Use for non-renderer actions: banners, polls, fanzone chips, etc.");
    Console.WriteLine("    --filter-subtext=<prefix>   With --dump-renderer: filter by headerSubtext/headerPrimaryText prefix.");
    Console.WriteLine("    --filter-has-field=<name>   With --dump-renderer/--dump-action: only include results where the given");
    Console.WriteLine("                                field name exists anywhere in the JSON (at any nesting depth).");
    Console.WriteLine("    --limit=<n>                 Cap the number of dumped results. Useful for extracting one sample.");
    Console.WriteLine("    --dump-output=<path>        Write dumped JSON to a file instead of stdout.");
    Console.WriteLine();
    Console.WriteLine("  Examples:");
    Console.WriteLine("    # Count everything in a directory of logs:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count logs/");
    Console.WriteLine();
    Console.WriteLine("    # Extract all fanzone chip actions as JSON:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count --dump-action=showFanzoneTickerChipCommand logs/");
    Console.WriteLine();
    Console.WriteLine("    # Extract banner redirect actions to a file:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count --dump-action=addBannerToLiveChatCommand --dump-output=banners.json log.jsonl");
    Console.WriteLine();
    Console.WriteLine("    # Extract membership upgrades by prefix filter:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count --dump-renderer=liveChatMembershipItemRenderer --filter-subtext=\"Upgraded\" --dump-output=upgrades.json log.json");
    Console.WriteLine();
    Console.WriteLine("    # Find first gift message with an authorAvatar (for test data extraction):");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count --dump-renderer=giftMessageViewModel --filter-has-field=authorAvatar --limit=1 logs/");
    Console.WriteLine();
    Console.WriteLine("    # Find paid stickers with a lowerBumper:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- count --dump-renderer=liveChatPaidStickerRenderer --filter-has-field=lowerBumper --limit=3 --dump-output=stickers_bumper.json logs/");
    Console.WriteLine();
    Console.WriteLine("━━━ analyze ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- analyze [options] <logPath|dir> [...]");
    Console.WriteLine();
    Console.WriteLine("  Compares observed renderer fields against C# response model baselines. Recursively");
    Console.WriteLine("  walks every action's entire JSON tree to surface new property names and new run");
    Console.WriteLine("  fields (text, bold, emoji, navigationEndpoint, etc.) at any nesting depth.");
    Console.WriteLine();
    Console.WriteLine("  Options:");
    Console.WriteLine("    -v, --verbose               Also print all known/expected fields (not just new ones).");
    Console.WriteLine();
    Console.WriteLine("  Report sections:");
    Console.WriteLine("    Action type counts          All top-level action types seen across all files.");
    Console.WriteLine("    Per-location renderer diffs  NEW fields vs. baseline for each renderer in each location.");
    Console.WriteLine("    Unknown Renderer Types       Renderer keys with no baseline at all.");
    Console.WriteLine("    Unknown Run Fields           Run-object properties not in KnownRunFields (any depth).");
    Console.WriteLine("    Unknown JSON Keys            Any property name not in AllKnownJsonKeys (any depth).");
    Console.WriteLine();
    Console.WriteLine("  Examples:");
    Console.WriteLine("    # Analyze a single new log:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- analyze logs/watch_20260420_060222.jsonl");
    Console.WriteLine();
    Console.WriteLine("    # Analyze all logs in a directory:");
    Console.WriteLine("    dotnet run --project YTLiveChat.Tools -- analyze logs/");
    Console.WriteLine();
    SnapshotMode.PrintSnapshotUsage();
}

static string BuildVariantSignature(string rendererType, JsonElement renderer, string source)
{
    List<string> parts = [$"src={source}", $"renderer={rendererType}", $"shape={BuildShapeSignature(renderer)}"];

    string? purchaseAmount = LogReader.TryGetSimpleText(renderer, "purchaseAmountText");
    if (!string.IsNullOrWhiteSpace(purchaseAmount))
    {
        parts.Add($"purchase={NormalizeText(purchaseAmount)}");
    }

    string? detailText = LogReader.TryGetSimpleText(renderer, "detailText");
    if (!string.IsNullOrWhiteSpace(detailText))
    {
        parts.Add($"detail={NormalizeText(detailText)}");
    }

    string? headerSubtextRuns = TryGetRunsTemplate(renderer, "headerSubtext");
    if (!string.IsNullOrWhiteSpace(headerSubtextRuns))
    {
        parts.Add($"headerSubtextRuns={headerSubtextRuns}");
    }

    string? subtextRuns = TryGetRunsTemplate(renderer, "subtext");
    if (!string.IsNullOrWhiteSpace(subtextRuns))
    {
        parts.Add($"subtextRuns={subtextRuns}");
    }

    string? primaryTextRuns = TryGetRunsTemplate(renderer, "primaryText");
    if (!string.IsNullOrWhiteSpace(primaryTextRuns))
    {
        parts.Add($"primaryTextRuns={primaryTextRuns}");
    }

    string? messageRuns = TryGetRunsTemplate(renderer, "message");
    if (!string.IsNullOrWhiteSpace(messageRuns))
    {
        parts.Add($"messageRuns={messageRuns}");
    }

    string? stickerLabel = LogReader.TryGetPathText(
        renderer,
        "sticker",
        "accessibility",
        "accessibilityData",
        "label"
    );
    if (!string.IsNullOrWhiteSpace(stickerLabel))
    {
        parts.Add($"stickerLabel={NormalizeText(stickerLabel)}");
    }

    return string.Join(" | ", parts);
}

static string BuildShapeSignature(JsonElement element)
{
    HashSet<string> shapePaths = new(StringComparer.Ordinal);
    CollectShapePaths(element, "$", shapePaths, 0, 10);
    string joined = string.Join(';', shapePaths.OrderBy(x => x, StringComparer.Ordinal));
    ulong hash = Fnv1a64(joined);
    return $"paths={shapePaths.Count},hash={hash:x16}";
}

static void CollectShapePaths(
    JsonElement element,
    string path,
    HashSet<string> shapePaths,
    int depth,
    int maxDepth
)
{
    if (depth >= maxDepth)
    {
        _ = shapePaths.Add($"{path}:<max-depth>");
        return;
    }

    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string childPath = $"{path}.{property.Name}";
                _ = shapePaths.Add(childPath);
                CollectShapePaths(property.Value, childPath, shapePaths, depth + 1, maxDepth);
            }

            return;

        case JsonValueKind.Array:
            _ = shapePaths.Add($"{path}[]");
            foreach (JsonElement item in element.EnumerateArray())
            {
                CollectShapePaths(item, $"{path}[]", shapePaths, depth + 1, maxDepth);
            }

            return;

        default:
            _ = shapePaths.Add($"{path}:{element.ValueKind}");
            return;
    }
}

static ulong Fnv1a64(string value)
{
    const ulong offsetBasis = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    ulong hash = offsetBasis;
    foreach (char c in value)
    {
        hash ^= c;
        hash *= prime;
    }

    return hash;
}

static string? TryGetRunsTemplate(JsonElement container, string richTextPropertyName)
{
    if (
        !container.TryGetProperty(richTextPropertyName, out JsonElement richText)
        || !richText.TryGetProperty("runs", out JsonElement runs)
        || runs.ValueKind != JsonValueKind.Array
    )
    {
        return null;
    }

    List<string> tokens = [];
    foreach (JsonElement run in runs.EnumerateArray())
    {
        if (run.ValueKind != JsonValueKind.Object)
        {
            tokens.Add("<?>");
            continue;
        }

        if (run.TryGetProperty("emoji", out JsonElement emoji))
        {
            string? shortcut = LogReader.TryGetPathText(emoji, "shortcuts", "0");
            tokens.Add(shortcut == null ? "<emoji>" : $"<emoji:{NormalizeText(shortcut)}>");
            continue;
        }

        if (run.TryGetProperty("text", out JsonElement textElement))
        {
            string text = textElement.GetString() ?? string.Empty;
            tokens.Add($"<text:{NormalizeText(text)}>");
            continue;
        }

        if (run.TryGetProperty("navigationEndpoint", out _))
        {
            tokens.Add("<endpoint>");
            continue;
        }

        tokens.Add("<object>");
    }

    return $"{tokens.Count}:{string.Join('+', tokens)}";
}

static string NormalizeText(string value)
{
    string compact = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    return compact.Length > 60 ? compact[..60] + "..." : compact;
}

static void RecordVariant(
    Dictionary<string, int> variantCounts,
    Dictionary<string, string> variantSamples,
    string key,
    JsonElement renderer
)
{
    Increment(variantCounts, key);
    if (variantSamples.ContainsKey(key))
    {
        return;
    }

    string json = JsonSerializer.Serialize(renderer);
    if (json.Length > 650)
    {
        json = json[..650] + "...";
    }

    variantSamples[key] = json;
}

static void PrintSorted(Dictionary<string, int> counts)
{
    foreach (
        KeyValuePair<string, int> kv in counts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
    )
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
    }
}

static void PrintVariants(
    Dictionary<string, int> variantCounts,
    Dictionary<string, string> variantSamples,
    int maxRows
)
{
    int row = 0;
    foreach (
        KeyValuePair<string, int> kv in variantCounts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
    )
    {
        if (row >= maxRows)
        {
            break;
        }

        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
        if (variantSamples.TryGetValue(kv.Key, out string? sample))
        {
            Console.WriteLine($"        sample: {sample}");
        }

        row++;
    }
}

static void Increment(Dictionary<string, int> dict, string key) =>
    dict[key] = dict.TryGetValue(key, out int value) ? value + 1 : 1;

internal sealed record Options(
    IReadOnlyList<string> Paths,
    bool EnableVariants,
    int MaxVariantRows,
    string? DumpRenderer,
    string? DumpAction,
    string? FilterSubtext,
    string? FilterHasField,
    int? DumpLimit,
    string? DumpOutput
);
