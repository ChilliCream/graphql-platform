namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageBodies
{
    private static readonly byte[] _defaultPong =
    [
        (byte)'{',
        (byte)'"',
        (byte)'t',
        (byte)'y',
        (byte)'p',
        (byte)'e',
        (byte)'"',
        (byte)':',
        (byte)'"',
        (byte)'p',
        (byte)'o',
        (byte)'n',
        (byte)'g',
        (byte)'"',
        (byte)'}',
    ];

    private static readonly byte[] _defaultPing =
    [
        (byte)'{',
        (byte)'"',
        (byte)'t',
        (byte)'y',
        (byte)'p',
        (byte)'e',
        (byte)'"',
        (byte)':',
        (byte)'"',
        (byte)'p',
        (byte)'i',
        (byte)'n',
        (byte)'g',
        (byte)'"',
        (byte)'}',
    ];

    public static ReadOnlyMemory<byte> DefaultPing => _defaultPing;

    public static ReadOnlyMemory<byte> DefaultPong => _defaultPong;
}
