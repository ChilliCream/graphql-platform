using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Transport.Sockets.GraphQLWS;

public sealed class ConnectionInitMessage : IMessage
{
    public ConnectionInitMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => "connection_init";

    public IDictionary<string, object?>? Payload { get; }

    public static ConnectionInitMessage Default { get; } = new ConnectionInitMessage(null);
}

public sealed class ConnectionAckMessage : IMessage
{
    public ConnectionAckMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => "connection_ack";

    public IDictionary<string, object?>? Payload { get; }

    public static ConnectionAckMessage Default { get; } = new ConnectionAckMessage(null);
}

public sealed class PingMessage : IMessage
{
    public PingMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => "ping";

    public IDictionary<string, object?>? Payload { get; }

    public static PingMessage Default { get; } = new PingMessage(null);
}

public sealed class PongMessage : IMessage
{
    public PongMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => "pong";

    public IDictionary<string, object?>? Payload { get; }

    public static PongMessage Default { get; } = new PongMessage(null);
}

public sealed class Sub : IMessage
{
    public Pong(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => "pong";

    public IDictionary<string, object?>? Payload { get; }

    public static Pong Default { get; } = new Pong(null);
}

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

    private static ConnectionInit ParseConnectionInit(JsonElement element)
    {
        if (element.TryGetProperty("payload", out var payloadValue))
        {
            return new ConnectionInit(ParseDictionary(payloadValue));
        }
        else
        {
            return ConnectionInit.Default;
        }
    }

    private static ConnectionAck ParseConnectionAck(JsonElement element)
    {
        if (element.TryGetProperty("payload", out var payloadValue))
        {
            return new ConnectionAck(ParseDictionary(payloadValue));
        }
        else
        {
            return ConnectionAck.Default;
        }
    }

    private static Dictionary<string, object?>? ParseDictionary(JsonElement element)
        => throw new Exception();
}