namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class Utf8Messages
{
    public static ReadOnlySpan<byte> ConnectionInitialize => "connection_init"u8;

    public static ReadOnlySpan<byte> ConnectionAccept => "connection_ack"u8;

    public static ReadOnlySpan<byte> Subscribe => "subscribe"u8;

    public static ReadOnlySpan<byte> Next => "next"u8;

    public static ReadOnlySpan<byte> Error => "error"u8;

    public static ReadOnlySpan<byte> Complete => "complete"u8;

    public static ReadOnlySpan<byte> Ping => "ping"u8;

    public static ReadOnlySpan<byte> Pong => "pong"u8;
}
