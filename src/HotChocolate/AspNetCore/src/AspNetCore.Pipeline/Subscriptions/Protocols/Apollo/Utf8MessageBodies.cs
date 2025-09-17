namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageBodies
{
    private static readonly byte[] s_keepAlive = """{"type":"ka"}"""u8.ToArray();

    public static ReadOnlyMemory<byte> KeepAlive => s_keepAlive;
}
