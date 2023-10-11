namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageProperties
{

    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> Id
        => "id"u8;

    public static ReadOnlySpan<byte> Type
        => "type"u8;
}
