namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageBodies
{
    private static readonly byte[] s_keepAlive =
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
        (byte)'}'
    ];

    public static ReadOnlyMemory<byte> KeepAlive => s_keepAlive;
}
