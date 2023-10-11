namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageBodies
{
    private static readonly byte[] _keepAlive = "{\"type\":\"ka\"}"u8.ToArray();

    public static ReadOnlyMemory<byte> KeepAlive => _keepAlive;
}
