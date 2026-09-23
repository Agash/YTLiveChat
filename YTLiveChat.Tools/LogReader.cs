using System.Text;
using System.Text.Json;

/// <summary>
/// Shared utilities for reading YTLiveChat log files (.json / .jsonl).
/// All parsing entry points live here so the analyze and default modes share identical I/O logic.
/// </summary>
internal static class LogReader
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads every action element from a log file.
    /// Handles four formats transparently:
    ///   • .jsonl — one compact action JSON per line (watch-mode output)
    ///   • JSON array of raw InnerTube response objects
    ///   • Concatenated JSON objects (legacy raw captures)
    ///   • JSON array of direct action objects
    /// </summary>
    public static IEnumerable<JsonElement> ReadActions(string path)
    {
        if (path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                using JsonDocument doc = JsonDocument.Parse(
                    trimmed,
                    new JsonDocumentOptions { AllowTrailingCommas = true }
                );
                foreach (JsonElement action in ExtractActions(doc.RootElement))
                    yield return action.Clone();
            }

            yield break;
        }

        // .json handling
        string json = File.ReadAllText(path, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(json))
            yield break;

        string jsonTrimmed = json.TrimStart();

        if (jsonTrimmed.StartsWith('['))
        {
            // JSON array — either array-of-responses or array-of-direct-actions
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in root.EnumerateArray())
                {
                    foreach (JsonElement action in ExtractActions(element))
                        yield return action.Clone();
                }
            }

            yield break;
        }

        // Concatenated JSON objects (Utf8JsonReader streams them one by one).
        // Utf8JsonReader is a ref struct and cannot cross a yield boundary,
        // so we materialise all actions before yielding.
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new(utf8, new JsonReaderOptions { AllowTrailingCommas = true });
        List<JsonElement> concatenatedActions = [];
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                continue;
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            foreach (JsonElement action in ExtractActions(doc.RootElement))
                concatenatedActions.Add(action.Clone());
        }

        foreach (JsonElement action in concatenatedActions)
            yield return action;
    }

    // ── Navigation helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the primary action/command key name from a top-level action element.
    /// Prefers *Action / *Command suffixed keys; falls back to the first non-tracking key.
    /// Returns null when the element is purely clickTrackingParams (no payload).
    /// </summary>
    public static string? GetActionType(JsonElement action)
    {
        if (action.ValueKind != JsonValueKind.Object)
            return null;

        string? fallback = null;
        foreach (JsonProperty prop in action.EnumerateObject())
        {
            if (
                prop.Name.EndsWith("Action", StringComparison.Ordinal)
                || prop.Name.EndsWith("Command", StringComparison.Ordinal)
            )
            {
                return prop.Name;
            }

            if (prop.Name != "clickTrackingParams")
                fallback ??= prop.Name;
        }

        return fallback;
    }

    /// <summary>
    /// Extracts the renderer key from a direct action (used by watch mode for renderer filtering).
    /// </summary>
    public static string GetRendererKey(JsonElement action)
    {
        // addChatItemAction.item.<rendererKey>
        return
            action.TryGetProperty("addChatItemAction", out JsonElement addChat)
            && addChat.TryGetProperty("item", out JsonElement item)
            && TryGetSingleRenderer(item, out string? r, out _)
            && r != null
            ? r
            : string.Empty;
    }

    /// <summary>
    /// Extracts the renderer type from an addChatItemAction item for watch-mode filtering.
    /// </summary>
    public static string? GetRendererTypeFromItem(JsonElement action)
    {
        return
            action.TryGetProperty("addChatItemAction", out JsonElement addChat)
            && addChat.TryGetProperty("item", out JsonElement item)
            && TryGetSingleRenderer(item, out string? r, out _)
            ? r
            : null;
    }

    /// <summary>
    /// Picks the single renderer property from a container object (e.g. addChatItemAction.item).
    /// Returns false when the container is empty or has no object property.
    /// </summary>
    public static bool TryGetSingleRenderer(
        JsonElement container,
        out string? rendererName,
        out JsonElement rendererValue
    )
    {
        rendererName = null;
        rendererValue = default;

        if (container.ValueKind != JsonValueKind.Object)
            return false;

        JsonProperty first = container.EnumerateObject().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first.Name))
            return false;

        rendererName = first.Name;
        rendererValue = first.Value;
        return true;
    }

    /// <summary>
    /// Navigates ticker item → showItemEndpoint → showLiveChatItemEndpoint → renderer
    /// and returns the nested renderer name and element.
    /// </summary>
    public static bool TryGetNestedShowRenderer(
        JsonElement tickerItem,
        out string? rendererName,
        out JsonElement rendererValue
    )
    {
        rendererName = null;
        rendererValue = default;

        foreach (JsonProperty tickerProp in tickerItem.EnumerateObject())
        {
            JsonElement val = tickerProp.Value;
            if (
                !val.TryGetProperty("showItemEndpoint", out JsonElement showEndpoint)
                || !showEndpoint.TryGetProperty(
                    "showLiveChatItemEndpoint",
                    out JsonElement showLive
                )
                || !showLive.TryGetProperty("renderer", out JsonElement rendererObj)
                || rendererObj.ValueKind != JsonValueKind.Object
            )
            {
                continue;
            }

            if (TryGetSingleRenderer(rendererObj, out rendererName, out rendererValue))
                return true;
        }

        return false;
    }

    // ── Text helpers ──────────────────────────────────────────────────────────

    public static string? TryGetSimpleText(JsonElement container, string propertyName) =>
        container.TryGetProperty(propertyName, out JsonElement val)
        && val.TryGetProperty("simpleText", out JsonElement st)
            ? st.GetString()
            : null;

    public static string? TryGetRunsAsPlainText(JsonElement container, string propertyName)
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
                _ = sb.Append(text.GetString());
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    public static string? TryGetPathText(JsonElement root, params string[] path)
    {
        JsonElement current = root;
        foreach (string segment in path)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                if (
                    !int.TryParse(segment, out int idx)
                    || idx < 0
                    || idx >= current.GetArrayLength()
                )
                    return null;
                current = current[idx];
                continue;
            }

            if (
                current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current)
            )
                return null;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    /// <summary>
    /// Produces a compact, truncated JSON summary of a value for display in reports.
    /// </summary>
    public static string SummarizeValue(JsonElement element, int maxLength = 90)
    {
        string json = JsonSerializer.Serialize(element);
        return json.Length <= maxLength ? json : json[..maxLength] + "…";
    }

    /// <summary>
    /// Expands a list of paths: directories are expanded to all *.jsonl files within them
    /// (non-recursive, sorted by name). File paths are returned as-is.
    /// </summary>
    public static IEnumerable<string> ExpandPaths(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (Directory.Exists(path))
            {
                foreach (
                    string file in Directory
                        .EnumerateFiles(path, "*.jsonl")
                        .OrderBy(f => f, StringComparer.Ordinal)
                )
                    yield return file;
            }
            else
            {
                yield return path;
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static IEnumerable<JsonElement> ExtractActions(JsonElement response)
    {
        // Standard InnerTube response path
        if (
            response.TryGetProperty("continuationContents", out JsonElement cc)
            && cc.TryGetProperty("liveChatContinuation", out JsonElement lcc)
            && lcc.TryGetProperty("actions", out JsonElement actions)
            && actions.ValueKind == JsonValueKind.Array
        )
        {
            foreach (JsonElement action in actions.EnumerateArray())
                yield return action;

            yield break;
        }

        // Direct action object (watch-mode JSONL)
        if (response.ValueKind == JsonValueKind.Object && IsDirectAction(response))
            yield return response;
    }

    private static bool IsDirectAction(JsonElement element)
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
}
