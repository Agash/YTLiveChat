using System.Text;
using System.Text.Json;

if (args.Length > 0 && args[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
{
    return await WatchMode.RunAsync(args[1..]);
}

Options options = ParseOptions(args);
if (options.Paths.Count == 0)
{
    options = PromptOptionsInteractive();
    if (options.Paths.Count == 0)
    {
        PrintUsage();
        return 1;
    }
}

// Dump mode: collect full untruncated renderer JSON for extraction
List<JsonElement> dumpedRenderers = [];

HashSet<string> knownActionTypes =
[
    "addChatItemAction",
    "addLiveChatTickerItemAction",
    "removeChatItemAction",
    "replaceChatItemAction",
    "removeChatItemByAuthorAction",
    "markChatItemsByAuthorAsDeletedAction",
    "changeEngagementPanelVisibilityAction",
    "signalAction",
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
        foreach (JsonElement action in ReadActionsFromLog(path))
        {
            string? actionType = GetActionType(action);
            if (string.IsNullOrWhiteSpace(actionType))
            {
                Increment(actionCounts, "<invalid-action>");
                continue;
            }

            Increment(actionCounts, actionType);

            if (
                actionType == "addChatItemAction"
                && action.TryGetProperty("addChatItemAction", out JsonElement addChat)
                && addChat.TryGetProperty("item", out JsonElement item)
                && item.ValueKind == JsonValueKind.Object
                && TryGetSingleRenderer(item, out string? rendererType, out JsonElement rendererValue)
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
                if (TryGetSingleRenderer(tickerItem, out string? tickerRenderer, out _))
                {
                    Increment(tickerRendererCounts, tickerRenderer!);
                }

                if (
                    TryGetNestedShowRenderer(
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
Console.WriteLine("== Unknown Actions (vs known parser surface) ==");
foreach (
    KeyValuePair<string, int> kv in actionCounts
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key, StringComparer.Ordinal)
)
{
    if (!knownActionTypes.Contains(kv.Key))
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
    Console.WriteLine($"== Dump: {options.DumpRenderer}{(options.FilterSubtext != null ? $" (headerSubtext prefix: \"{options.FilterSubtext}\")" : "")} ==");
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

    if (options.FilterSubtext != null)
    {
        string? headerSubtext = TryGetSimpleText(rendererValue, "headerSubtext")
            ?? TryGetRunsAsPlainText(rendererValue, "headerSubtext")
            ?? TryGetSimpleText(rendererValue, "headerPrimaryText")
            ?? TryGetRunsAsPlainText(rendererValue, "headerPrimaryText");

        if (
            headerSubtext == null
            || !headerSubtext.StartsWith(options.FilterSubtext, StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }
    }

    dumpedRenderers.Add(rendererValue.Clone());
}

static string? TryGetRunsAsPlainText(JsonElement container, string propertyName)
{
    if (
        !container.TryGetProperty(propertyName, out JsonElement richText)
        || !richText.TryGetProperty("runs", out JsonElement runs)
        || runs.ValueKind != JsonValueKind.Array
    )
    {
        return null;
    }

    StringBuilder sb = new();
    foreach (JsonElement run in runs.EnumerateArray())
    {
        if (run.TryGetProperty("text", out JsonElement text))
        {
            sb.Append(text.GetString());
        }
    }

    return sb.Length > 0 ? sb.ToString() : null;
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
    string? filterSubtext = null;
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

        const string filterSubtextPrefix = "--filter-subtext=";
        if (arg.StartsWith(filterSubtextPrefix, StringComparison.OrdinalIgnoreCase))
        {
            filterSubtext = arg[filterSubtextPrefix.Length..];
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

    return new(paths, enableVariants, maxVariantRows, dumpRenderer, filterSubtext, dumpOutput);
}

static Options PromptOptionsInteractive()
{
    Console.WriteLine("No log paths were passed via args. Switching to interactive mode.");
    Console.WriteLine("Enter one or more log paths separated by ';' or ',':");
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

    return new(paths, enableVariants, maxVariantRows, DumpRenderer: null, FilterSubtext: null, DumpOutput: null);
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine(
        "  dotnet run --project YTLiveChat.Tools -- [options] <logPath1> [logPath2 ...]"
    );
    Console.WriteLine(
        "  dotnet run --project YTLiveChat.Tools -- watch [watch-options] <@handle|UCxxx|liveId> [...]"
    );
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine(
        "  --variants                    Enables variant signatures for memberships, super chats/stickers, and unknown renderers."
    );
    Console.WriteLine("  --max-variant-rows=<n>        Limits printed rows per variant section. Default: 25.");
    Console.WriteLine(
        "  --dump-renderer=<name>        Extract full untruncated JSON for all matching renderer types."
    );
    Console.WriteLine(
        "  --filter-subtext=<prefix>     When used with --dump-renderer, only include items whose headerSubtext"
    );
    Console.WriteLine(
        "                                or headerPrimaryText starts with the given prefix (case-insensitive)."
    );
    Console.WriteLine(
        "  --dump-output=<path>          Write dumped JSON array to a file instead of stdout."
    );
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine(
        "  # Find all membership item renderers in a log:"
    );
    Console.WriteLine(
        "  dotnet run --project YTLiveChat.Tools -- --dump-renderer=liveChatMembershipItemRenderer log.json"
    );
    Console.WriteLine();
    Console.WriteLine(
        "  # Extract only membership upgrade events (by headerSubtext prefix) to a file:"
    );
    Console.WriteLine(
        "  dotnet run --project YTLiveChat.Tools -- --dump-renderer=liveChatMembershipItemRenderer --filter-subtext=\"Upgraded\" --dump-output=upgrade_events.json log.json"
    );
}

static bool TryGetNestedShowRenderer(
    JsonElement tickerItem,
    out string? rendererName,
    out JsonElement rendererValue
)
{
    rendererName = null;
    rendererValue = default;

    foreach (JsonProperty tickerProperty in tickerItem.EnumerateObject())
    {
        JsonElement value = tickerProperty.Value;
        if (
            !value.TryGetProperty("showItemEndpoint", out JsonElement showItemEndpoint)
            || !showItemEndpoint.TryGetProperty("showLiveChatItemEndpoint", out JsonElement showLive)
            || !showLive.TryGetProperty("renderer", out JsonElement rendererObj)
            || rendererObj.ValueKind != JsonValueKind.Object
        )
        {
            continue;
        }

        if (TryGetSingleRenderer(rendererObj, out rendererName, out rendererValue))
        {
            return true;
        }
    }

    return false;
}

static bool TryGetSingleRenderer(
    JsonElement container,
    out string? rendererName,
    out JsonElement rendererValue
)
{
    rendererName = null;
    rendererValue = default;

    if (container.ValueKind != JsonValueKind.Object)
    {
        return false;
    }

    JsonProperty first = container.EnumerateObject().FirstOrDefault();
    if (string.IsNullOrWhiteSpace(first.Name))
    {
        return false;
    }

    rendererName = first.Name;
    rendererValue = first.Value;
    return true;
}

static string BuildVariantSignature(string rendererType, JsonElement renderer, string source)
{
    List<string> parts = [$"src={source}", $"renderer={rendererType}", $"shape={BuildShapeSignature(renderer)}"];

    string? purchaseAmount = TryGetSimpleText(renderer, "purchaseAmountText");
    if (!string.IsNullOrWhiteSpace(purchaseAmount))
    {
        parts.Add($"purchase={NormalizeText(purchaseAmount)}");
    }

    string? detailText = TryGetSimpleText(renderer, "detailText");
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

    string? stickerLabel = TryGetPathText(
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
            string? shortcut = TryGetPathText(emoji, "shortcuts", "0");
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

static string? TryGetSimpleText(JsonElement container, string propertyName)
{
    return container.TryGetProperty(propertyName, out JsonElement value)
        && value.TryGetProperty("simpleText", out JsonElement simpleText)
        ? simpleText.GetString()
        : null;
}

static string? TryGetPathText(JsonElement root, params string[] path)
{
    JsonElement current = root;
    foreach (string segment in path)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            if (!int.TryParse(segment, out int index))
            {
                return null;
            }

            if (index < 0 || index >= current.GetArrayLength())
            {
                return null;
            }

            current = current[index];
            continue;
        }

        if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
        {
            return null;
        }
    }

    return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
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

static string? GetActionType(JsonElement action)
{
    if (action.ValueKind != JsonValueKind.Object)
    {
        return null;
    }

    string? fallback = null;
    foreach (JsonProperty property in action.EnumerateObject())
    {
        fallback ??= property.Name;
        if (
            property.Name.EndsWith("Action", StringComparison.Ordinal)
            || property.Name.EndsWith("Command", StringComparison.Ordinal)
        )
        {
            return property.Name;
        }
    }

    return fallback;
}

static List<JsonElement> ReadActionsFromLog(string path)
{
    List<JsonElement> actionsOut = [];
    string json = File.ReadAllText(path, Encoding.UTF8);
    if (string.IsNullOrWhiteSpace(json))
    {
        return actionsOut;
    }

    string trimmed = json.TrimStart();
    if (trimmed.StartsWith('['))
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement response in root.EnumerateArray())
            {
                foreach (JsonElement action in ExtractActions(response))
                {
                    actionsOut.Add(action.Clone());
                }
            }
        }

        return actionsOut;
    }

    byte[] utf8 = Encoding.UTF8.GetBytes(json);
    Utf8JsonReader reader = new(utf8, new JsonReaderOptions { AllowTrailingCommas = true });
    while (reader.Read())
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            continue;
        }

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        foreach (JsonElement action in ExtractActions(doc.RootElement))
        {
            actionsOut.Add(action.Clone());
        }
    }

    return actionsOut;
}

static IEnumerable<JsonElement> ExtractActions(JsonElement response)
{
    // Normal InnerTube response path
    if (
        response.TryGetProperty("continuationContents", out JsonElement continuationContents)
        && continuationContents.TryGetProperty("liveChatContinuation", out JsonElement liveChat)
        && liveChat.TryGetProperty("actions", out JsonElement actions)
        && actions.ValueKind == JsonValueKind.Array
    )
    {
        foreach (JsonElement action in actions.EnumerateArray())
        {
            yield return action;
        }

        yield break;
    }

    // Direct action object — from watch mode JSONL capture files
    if (response.ValueKind == JsonValueKind.Object && IsDirectAction(response))
    {
        yield return response;
    }
}

static bool IsDirectAction(JsonElement element)
{
    foreach (JsonProperty prop in element.EnumerateObject())
    {
        if (
            prop.Name.EndsWith("Action", StringComparison.Ordinal)
            || prop.Name.EndsWith("Command", StringComparison.Ordinal)
        )
        {
            return true;
        }
    }

    return false;
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
    string? FilterSubtext,
    string? DumpOutput
);
