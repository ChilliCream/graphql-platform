namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8Messages
{
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> ConnectionInitialize =>
        "connection_init"u8;

    public static ReadOnlySpan<byte> ConnectionAccept =>
        "connection_ack"u8;

    public static ReadOnlySpan<byte> ConnectionError =>
        "connection_error"u8;

    public static ReadOnlySpan<byte> ConnectionTerminate =>
        "connection_terminate"u8;

    public static ReadOnlySpan<byte> Start =>
        "start"u8;

    public static ReadOnlySpan<byte> Stop =>
        "stop"u8;

    public static ReadOnlySpan<byte> Data =>
        "data"u8;

    public static ReadOnlySpan<byte> Error =>
        "error"u8;

    public static ReadOnlySpan<byte> Complete =>
        "complete"u8;
}
