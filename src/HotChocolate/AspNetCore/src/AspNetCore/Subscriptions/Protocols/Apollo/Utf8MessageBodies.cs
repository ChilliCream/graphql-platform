namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageBodies
{
    private static readonly byte[] _keepAlive =
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
        (byte)'k',
        (byte)'a',
        (byte)'"',
        (byte)'}',
    ];

    public static ReadOnlyMemory<byte> KeepAlive => _keepAlive;
}
