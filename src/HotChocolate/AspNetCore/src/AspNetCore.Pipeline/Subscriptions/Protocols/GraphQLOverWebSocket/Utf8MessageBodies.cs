namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageBodies
{
    private static readonly byte[] s_defaultPong = """{"type":"pong"}"""u8.ToArray();

    private static readonly byte[] s_defaultPing = """{"type":"ping"}"""u8.ToArray();

    public static ReadOnlyMemory<byte> DefaultPing => s_defaultPing;

    public static ReadOnlyMemory<byte> DefaultPong => s_defaultPong;
}
