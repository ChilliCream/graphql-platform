namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageProperties
{
    public static ReadOnlySpan<byte> Type => "type"u8;

    public static ReadOnlySpan<byte> Payload => "payload"u8;
}
