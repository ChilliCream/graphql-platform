namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageBodies
{
    private static readonly byte[] _defaultPong = "{\"type\":\"pong\"}"u8.ToArray();

    private static readonly byte[] _defaultPing = "{\"type\":\"ping\"}"u8.ToArray();

    public static ReadOnlyMemory<byte> DefaultPing => _defaultPing;

    public static ReadOnlyMemory<byte> DefaultPong => _defaultPong;
}
