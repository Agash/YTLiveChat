using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;

/// <summary>
/// Records the shape of every chat response: each key path (object keys joined by '.', array elements as '[]') with
/// how often it appeared and one sample value. Written to a TSV every two minutes and when watching stops, so the
/// paths can be compared against what the library models: nothing in a response goes unseen, regular chat messages
/// included.
/// </summary>
internal sealed class SchemaRecorder : IDisposable
{
    private const int SampleLength = 160;
    private readonly ConcurrentDictionary<string, PathStats> _paths = new(StringComparer.Ordinal);
    private readonly string _outputPath;
    private readonly ITimer _flush;
    private long _responses;

    public SchemaRecorder(string outputPath)
    {
        _outputPath = outputPath;
        _flush = TimeProvider.System.CreateTimer(
            static state => ((SchemaRecorder)state!).Flush(),
            this,
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(2)
        );
    }

    public void Record(string rawJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(rawJson);
            Walk(document.RootElement, string.Empty);
            _ = Interlocked.Increment(ref _responses);
        }
        catch (JsonException)
        {
            // A response that is not JSON has no shape to record; the chat loop reports it.
        }
    }

    public void Flush()
    {
        StringBuilder builder = new();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"# responses\t{Interlocked.Read(ref _responses)}");
        _ = builder.AppendLine("path\tcount\tsample");
        foreach (KeyValuePair<string, PathStats> entry in _paths.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            _ = builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"{entry.Key}\t{entry.Value.Count}\t{entry.Value.Sample}"
            );
        }

        string temporary = _outputPath + ".tmp";
        File.WriteAllText(temporary, builder.ToString());
        File.Move(temporary, _outputPath, overwrite: true);
    }

    public void Dispose()
    {
        _flush.Dispose();
        Flush();
    }

    private void Walk(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    Walk(property.Value, path.Length == 0 ? property.Name : path + "." + property.Name);
                }

                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Walk(item, path + "[]");
                }

                break;
            default:
                string raw = element.GetRawText().Replace('\t', ' ').Replace('\n', ' ');
                _paths.AddOrUpdate(
                    path,
                    _ => new PathStats(1, raw.Length > SampleLength ? raw[..SampleLength] : raw),
                    static (_, stats) => stats with { Count = stats.Count + 1 }
                );
                break;
        }
    }

    private sealed record PathStats(long Count, string Sample);
}
