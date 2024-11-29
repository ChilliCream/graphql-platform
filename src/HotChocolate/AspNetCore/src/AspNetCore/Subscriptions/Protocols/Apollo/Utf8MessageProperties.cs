namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Utf8MessageProperties
{
    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> Id
        => [(byte)'i', (byte)'d',];

    public static ReadOnlySpan<byte> Type
        => [(byte)'t', (byte)'y', (byte)'p', (byte)'e',];
}
