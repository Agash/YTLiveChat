using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using YTLiveChat.Models.Response;

/// <summary>
/// Schema-check mode: reads a <c>.schema.tsv</c> written by <c>watch</c> and follows every recorded key path through
/// the library's response models (<see cref="LiveChatResponse"/> and the types its properties lead to, by their
/// <see cref="JsonPropertyNameAttribute"/>s). Reports each path whose first unmapped key the models would drop,
/// grouped by that key, with counts and a sample. Paths that reach a <see cref="JsonExtensionDataAttribute"/>
/// member are kept raw and listed separately.
/// </summary>
internal static class SchemaCheckMode
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || !File.Exists(args[0]))
        {
            Console.WriteLine("Usage: schemacheck <file.schema.tsv> [--min-count=n]");
            return 1;
        }

        long minCount = 1;
        foreach (string arg in args.Skip(1))
        {
            if (arg.StartsWith("--min-count=", StringComparison.Ordinal))
            {
                minCount = long.Parse(arg["--min-count=".Length..], CultureInfo.InvariantCulture);
            }
        }

        Dictionary<string, (long Count, string Sample, int Paths)> dropped = new(StringComparer.Ordinal);
        Dictionary<string, (long Count, int Paths)> raw = new(StringComparer.Ordinal);
        foreach (string line in File.ReadLines(args[0]))
        {
            if (line.StartsWith('#') || line.StartsWith("path\t", StringComparison.Ordinal))
            {
                continue;
            }

            string[] columns = line.Split('\t');
            if (columns.Length < 3)
            {
                continue;
            }

            long count = long.Parse(columns[1], CultureInfo.InvariantCulture);
            (Outcome outcome, string prefix) = Follow(typeof(LiveChatResponse), columns[0]);
            if (outcome == Outcome.Dropped)
            {
                dropped[prefix] = dropped.TryGetValue(prefix, out var d)
                    ? (d.Count + count, d.Sample, d.Paths + 1)
                    : (count, $"{columns[0]} = {columns[2]}", 1);
            }
            else if (outcome == Outcome.Raw)
            {
                raw[prefix] = raw.TryGetValue(prefix, out var r) ? (r.Count + count, r.Paths + 1) : (count, 1);
            }
        }

        Console.WriteLine($"DROPPED (first unmapped key; values, paths beneath, sample)");
        foreach ((string prefix, (long count, string sample, int paths)) in dropped.OrderBy(static d => d.Key, StringComparer.Ordinal))
        {
            if (count >= minCount)
            {
                Console.WriteLine($"{count,9} {paths,5}  {prefix}\n                  e.g. {sample}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("KEPT RAW (JsonExtensionData)");
        foreach ((string prefix, (long count, int paths)) in raw.OrderBy(static r => r.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"{count,9} {paths,5}  {prefix}");
        }

        return 0;
    }

    private enum Outcome
    {
        Mapped,
        Dropped,
        Raw,
    }

    private static (Outcome Outcome, string Prefix) Follow(Type root, string path)
    {
        Type current = root;
        string walked = string.Empty;
        foreach (string segment in path.Split('.'))
        {
            string key = segment.TrimEnd('[', ']');
            int arrays = (segment.Length - key.Length) / 2;
            walked = walked.Length == 0 ? key : walked + "." + key;

            PropertyInfo? property = current
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == key);
            if (property is null)
            {
                bool hasExtension = current
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(static p => p.GetCustomAttribute<JsonExtensionDataAttribute>() is not null);
                return (hasExtension ? Outcome.Raw : Outcome.Dropped, walked);
            }

            // A custom converter reads the value itself (message runs, for example); what it reads is not visible here.
            if (
                property.GetCustomAttribute<JsonConverterAttribute>() is not null
                || (ElementType(property.PropertyType) ?? property.PropertyType).GetCustomAttribute<JsonConverterAttribute>() is not null
            )
            {
                return (Outcome.Mapped, walked);
            }

            Type next = property.PropertyType;
            for (int i = 0; i < arrays; i++)
            {
                next = ElementType(next) ?? next;
            }

            current = Nullable.GetUnderlyingType(next) ?? next;
            if (current.IsPrimitive || current == typeof(string) || current.IsEnum || current == typeof(System.Text.Json.JsonElement))
            {
                return (Outcome.Mapped, walked);
            }
        }

        return (Outcome.Mapped, walked);
    }

    private static Type? ElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        Type? enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? (typeof(IEnumerable).IsAssignableFrom(type) ? typeof(object) : null);
    }
}
