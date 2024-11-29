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
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals(Utf8MessageProperties.IdProp))
            {
                reader.Read();

                if (reader.GetString() is { } result)
                {
                    return result;
                }
            }
        }

        throw ThrowHelper.MessageHasNoId();
    }
}
