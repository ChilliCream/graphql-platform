namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageProperties
{
    public static ReadOnlySpan<byte> Id => "id"u8;

    public static ReadOnlySpan<byte> Type => "type"u8;
}
