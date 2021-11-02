using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public class MessageParser
{
    public IMessage Parse(ReadOnlySpan<byte> message)
    {
        var reader = new Utf8JsonReader(message);
        var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return ParseMessage(root);
    }

    private static IMessage ParseMessage(JsonElement element)
    {
        if (element.TryGetProperty("type", out var value))
        {
            switch (value.GetString())
            {
                case "connection_init":
                    break;
            }
        }

        throw new Exception("");
    }

    private static ConnectionInitMessage ParseConnectionInit(JsonElement element)
    {
        if (element.TryGetProperty("payload", out var payloadValue))
        {
            return new ConnectionInitMessage(ParseDictionary(payloadValue));
        }
        else
        {
            return ConnectionInitMessage.Default;
        }
    }

    private static ConnectionAckMessage ParseConnectionAck(JsonElement element)
    {
        if (element.TryGetProperty("payload", out var payloadValue))
        {
            return new ConnectionAckMessage(ParseDictionary(payloadValue));
        }
        else
        {
            return ConnectionAckMessage.Default;
        }
    }

    private static Dictionary<string, object?>? ParseDictionary(JsonElement element)
        => throw new Exception();
}