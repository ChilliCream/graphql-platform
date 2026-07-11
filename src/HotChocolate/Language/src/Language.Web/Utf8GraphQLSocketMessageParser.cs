using System.Text.Json;

namespace HotChocolate.Language;

public readonly ref struct Utf8GraphQLSocketMessageParser
{
    private static readonly JsonReaderOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly ReadOnlySpan<byte> _messageData;

    public Utf8GraphQLSocketMessageParser(ReadOnlySpan<byte> messageData)
    {
        _messageData = messageData;
    }

    public readonly GraphQLSocketMessage ParseMessage()
    {
        var reader = new Utf8JsonReader(_messageData, s_jsonOptions);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Expected JSON object for socket message.");
        }

        string? type = null;
        string? id = null;
        var payload = default(ReadOnlySpan<byte>);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (reader.ValueTextEquals("type"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    type = reader.GetString();
                }
            }
            else if (reader.ValueTextEquals("id"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    id = reader.GetString();
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    id = null;
                }
            }
            else if (reader.ValueTextEquals("payload"u8))
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.Null)
                {
                    // Capture the raw bytes of the payload value
                    var start = (int)reader.TokenStartIndex;
                    reader.Skip();
                    var end = (int)reader.TokenStartIndex;
                    payload = _messageData.Slice(start, end - start);
                }
            }
            else
            {
                // Skip unknown properties
                reader.Read();
                reader.Skip();
            }
        }

        return new GraphQLSocketMessage(type, id, payload);
    }

    public static GraphQLSocketMessage ParseMessage(ReadOnlySpan<byte> messageData)
        => new Utf8GraphQLSocketMessageParser(messageData).ParseMessage();
}
