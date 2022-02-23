namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class Messages
{
    public const string ConnectionInitialize = "connection_init";
    public const string ConnectionAccept = "connection_ack";
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Subscribe = "subscribe";
    public const string Next = "next";
    public const string Error = "error";
    public const string Complete = "complete";
}


internal static class Utf8Messages
{
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> ConnectionInitialize =>
        new[]
        {
            (byte)'c',
            (byte)'o',
            (byte)'n',
            (byte)'n',
            (byte)'e',
            (byte)'c',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'_',
            (byte)'i',
            (byte)'n',
            (byte)'i',
            (byte)'t'
        };

    public static ReadOnlySpan<byte> Subscribe =>
        new[]
        {
            (byte)'s',
            (byte)'u',
            (byte)'b',
            (byte)'s',
            (byte)'c',
            (byte)'r',
            (byte)'i',
            (byte)'b',
            (byte)'e'
        };
}
