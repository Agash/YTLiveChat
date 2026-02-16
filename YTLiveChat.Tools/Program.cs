using System.Text;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run --project YTLiveChat.Tools -- <logPath1> [logPath2 ...]");
    return 1;
}

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

Dictionary<string, int> actionCounts = new(StringComparer.Ordinal);
Dictionary<string, int> rendererCounts = new(StringComparer.Ordinal);
Dictionary<string, int> tickerRendererCounts = new(StringComparer.Ordinal);
Dictionary<string, int> nestedRendererCounts = new(StringComparer.Ordinal);
List<string> parseErrors = [];

foreach (string path in args)
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

            if (actionType == "addChatItemAction"
                && action.TryGetProperty("addChatItemAction", out JsonElement addChat)
                && addChat.TryGetProperty("item", out JsonElement item)
                && item.ValueKind == JsonValueKind.Object)
            {
                string? rendererType = item.EnumerateObject().FirstOrDefault().Name;
                if (!string.IsNullOrWhiteSpace(rendererType))
                {
                    Increment(rendererCounts, rendererType);
                }
            }
            else if (actionType == "addLiveChatTickerItemAction"
                && action.TryGetProperty("addLiveChatTickerItemAction", out JsonElement tickerAction)
                && tickerAction.TryGetProperty("item", out JsonElement tickerItem)
                && tickerItem.ValueKind == JsonValueKind.Object)
            {
                string? tickerRenderer = tickerItem.EnumerateObject().FirstOrDefault().Name;
                if (!string.IsNullOrWhiteSpace(tickerRenderer))
                {
                    Increment(tickerRendererCounts, tickerRenderer);
                }

                if (TryGetNestedShowRenderer(tickerItem, out string? nestedRenderer) && nestedRenderer != null)
                {
                    Increment(nestedRendererCounts, nestedRenderer);
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
foreach (var kv in actionCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
{
    if (!knownActionTypes.Contains(kv.Key))
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
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

static bool TryGetNestedShowRenderer(JsonElement tickerItem, out string? renderer)
{
    renderer = null;

    foreach (JsonProperty tickerProperty in tickerItem.EnumerateObject())
    {
        JsonElement value = tickerProperty.Value;
        if (!value.TryGetProperty("showItemEndpoint", out JsonElement showItemEndpoint)
            || !showItemEndpoint.TryGetProperty("showLiveChatItemEndpoint", out JsonElement showLive)
            || !showLive.TryGetProperty("renderer", out JsonElement rendererObj)
            || rendererObj.ValueKind != JsonValueKind.Object)
        {
            continue;
        }

        renderer = rendererObj.EnumerateObject().FirstOrDefault().Name;
        return !string.IsNullOrWhiteSpace(renderer);
    }

    return false;
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
    if (trimmed.StartsWith("[", StringComparison.Ordinal))
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
    var reader = new Utf8JsonReader(utf8, new JsonReaderOptions { AllowTrailingCommas = true });
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
    if (!response.TryGetProperty("continuationContents", out JsonElement continuationContents)
        || !continuationContents.TryGetProperty("liveChatContinuation", out JsonElement liveChat)
        || !liveChat.TryGetProperty("actions", out JsonElement actions)
        || actions.ValueKind != JsonValueKind.Array)
    {
        yield break;
    }

    foreach (JsonElement action in actions.EnumerateArray())
    {
        yield return action;
    }
}

static void PrintSorted(Dictionary<string, int> counts)
{
    foreach (var kv in counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"{kv.Value,6}  {kv.Key}");
    }
}

static void Increment(Dictionary<string, int> dict, string key)
{
    if (dict.TryGetValue(key, out int value))
    {
        dict[key] = value + 1;
    }
    else
    {
        dict[key] = 1;
    }
}
