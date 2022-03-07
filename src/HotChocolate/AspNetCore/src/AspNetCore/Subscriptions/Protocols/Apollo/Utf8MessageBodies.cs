namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageBodies
{
    private static readonly byte[] _keepAlive =
    {
        (byte)'k',
        (byte)'a'
    };

    public static ReadOnlyMemory<byte> KeepAlive => _keepAlive;
}
