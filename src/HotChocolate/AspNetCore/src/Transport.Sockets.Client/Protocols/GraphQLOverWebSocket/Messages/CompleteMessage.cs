using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal sealed class CompleteMessage : IDataMessage
{
    private CompleteMessage(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Type => Messages.Complete;

    public void Dispose()
    {
        // a complete message carries no pooled buffers, so there is nothing to release.
    }

    public static CompleteMessage From(ReadOnlySequence<byte> message)
    {
        var id = ParseId(message);

        return new CompleteMessage(id);
    }

    private static string ParseId(ReadOnlySequence<byte> payload)
    {
        var reader = new Utf8JsonReader(payload);

        while (reader.Read())
        {
            if (reader.CurrentDepth == 1
                && reader.TokenType == JsonTokenType.PropertyName
                && reader.ValueTextEquals(Utf8MessageProperties.IdProp))
            {
                if (reader.Read()
                    && reader.TokenType == JsonTokenType.String
                    && reader.GetString() is { } result)
                {
                    return result;
                }
            }
        }

        throw ThrowHelper.MessageHasNoId();
    }
}
