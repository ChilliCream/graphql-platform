namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

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
            (byte)'t',
        };

    public static ReadOnlySpan<byte> ConnectionAccept =>
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
            (byte)'a',
            (byte)'c',
            (byte)'k',
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
            (byte)'e',
        };

    public static ReadOnlySpan<byte> Next =>
        new[]
        {
            (byte)'n',
            (byte)'e',
            (byte)'x',
            (byte)'t',
        };

    public static ReadOnlySpan<byte> Error =>
        new[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r',
        };

    public static ReadOnlySpan<byte> Complete =>
        new[]
        {
            (byte)'c',
            (byte)'o',
            (byte)'m',
            (byte)'p',
            (byte)'l',
            (byte)'e',
            (byte)'t',
            (byte)'e',
        };

    public static ReadOnlySpan<byte> Ping =>
        new[]
        {
            (byte)'p',
            (byte)'i',
            (byte)'n',
            (byte)'g',
        };

    public static ReadOnlySpan<byte> Pong =>
        new[]
        {
            (byte)'p',
            (byte)'o',
            (byte)'n',
            (byte)'g',
        };
}
