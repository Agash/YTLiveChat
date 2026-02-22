using System.Text.Json;
using System.Text.Json.Serialization;

using YTLiveChat.Models.Response;

using static YTLiveChat.Helpers.YTLiveChatJsonSerializerContext;

namespace YTLiveChat.Helpers;

internal class MessageRunConverter : JsonConverter<MessageRun>
{
    public override MessageRun? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);

        return doc.RootElement.TryGetProperty("emoji", out _)
                ? JsonSerializer.Deserialize(doc.RootElement, Default.MessageEmoji)
            : doc.RootElement.TryGetProperty("text", out _)
                ? (MessageRun?)JsonSerializer.Deserialize(doc.RootElement, Default.MessageText)
            : throw new JsonException("Invalid MessageRun format.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        MessageRun value,
        JsonSerializerOptions options
    )
    {
        switch (value)
        {
            case MessageEmoji emoji:
                JsonSerializer.Serialize(writer, emoji, Default.MessageEmoji);
                break;
            case MessageText text:
                JsonSerializer.Serialize(writer, text, Default.MessageText);
                break;
            default:
                throw new JsonException($"Unknown MessageRun type: {value.GetType()}");
        }
    }
}
