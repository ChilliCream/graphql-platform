namespace StrawberryShake.Transport.WebSockets.Protocols;

public static class GraphQLWebSocketMessageTypeSpans
{
    public static ReadOnlySpan<byte> ConnectionInitialize =>
    [
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
    ];

    public static ReadOnlySpan<byte> ConnectionAccept =>
    [
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
    ];

    public static ReadOnlySpan<byte> ConnectionError =>
    [
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
        (byte)'e',
        (byte)'r',
        (byte)'r',
        (byte)'o',
        (byte)'r',
    ];

    public static ReadOnlySpan<byte> KeepAlive =>
    [
        (byte)'k',
        (byte)'a',
    ];

    public static ReadOnlySpan<byte> ConnectionTerminate =>
    [
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
        (byte)'t',
        (byte)'e',
        (byte)'r',
        (byte)'m',
        (byte)'i',
        (byte)'n',
        (byte)'a',
        (byte)'t',
        (byte)'e',
    ];

    public static ReadOnlySpan<byte> Start =>
    [
        (byte)'s',
        (byte)'t',
        (byte)'a',
        (byte)'r',
        (byte)'t',
    ];

    public static ReadOnlySpan<byte> Data =>
    [
        (byte)'d',
        (byte)'a',
        (byte)'t',
        (byte)'a',
    ];

    public static ReadOnlySpan<byte> Error =>
    [
        (byte)'e',
        (byte)'r',
        (byte)'r',
        (byte)'o',
        (byte)'r',
    ];

    public static ReadOnlySpan<byte> Complete =>
    [
        (byte)'c',
        (byte)'o',
        (byte)'m',
        (byte)'p',
        (byte)'l',
        (byte)'e',
        (byte)'t',
        (byte)'e',
    ];

    public static ReadOnlySpan<byte> Stop =>
    [
        (byte)'s',
        (byte)'t',
        (byte)'o',
        (byte)'p',
    ];
}
