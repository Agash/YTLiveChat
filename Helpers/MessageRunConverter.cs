using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTLiveChat.Helpers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using YTLiveChat.Models.Response;

internal class MessageRunConverter : JsonConverter<MessageRun>
{
    public override MessageRun? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);

        if (doc.RootElement.TryGetProperty("emoji", out _))
        {
            return JsonSerializer.Deserialize<MessageEmoji>(doc.RootElement, options);
        }
        else if (doc.RootElement.TryGetProperty("text", out _))
        {
            return JsonSerializer.Deserialize<MessageText>(doc.RootElement, options);
        }
        else
        {
            throw new JsonException("Invalid MessageRun format.");
        }
    }

    public override void Write(Utf8JsonWriter writer, MessageRun value, JsonSerializerOptions options)
    {
        throw new NotImplementedException(); // Implement if needed
    }
}