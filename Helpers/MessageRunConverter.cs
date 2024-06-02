using System.Text.Json;
using System.Text.Json.Serialization;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Helpers;
internal class MessageRunConverter : JsonConverter<MessageRun>
{
    public override MessageRun? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);

        if (doc.RootElement.TryGetProperty("emoji", out _))
        {
            return JsonSerializer.Deserialize<MessageEmoji>(doc.RootElement, options);
        }
        else
        {
            return doc.RootElement.TryGetProperty("text", out _)
                ? (MessageRun?)JsonSerializer.Deserialize<MessageText>(doc.RootElement, options)
                : throw new JsonException("Invalid MessageRun format.");
        }
    }

    public override void Write(Utf8JsonWriter writer, MessageRun value, JsonSerializerOptions options) => throw new NotImplementedException(); // Implement if needed
}