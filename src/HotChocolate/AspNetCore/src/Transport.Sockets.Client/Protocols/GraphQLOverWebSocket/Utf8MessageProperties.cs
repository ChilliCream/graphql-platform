using System;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageProperties
{

    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> IdProp
        => new[]
        {
            (byte)'i',
            (byte)'d'
        };

    public static ReadOnlySpan<byte> TypeProp
        => new[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

    public static ReadOnlySpan<byte> PayloadProp
        => new[]
        {
            (byte)'p',
            (byte)'a',
            (byte)'y',
            (byte)'l',
            (byte)'o',
            (byte)'a',
            (byte)'d'
        };

    public static ReadOnlySpan<byte> DataProp
        => new[]
        {
            (byte)'d',
            (byte)'a',
            (byte)'t',
            (byte)'a'
        };

    public static ReadOnlySpan<byte> ErrorsProp
        => new[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r',
            (byte)'s'
        };

    public static ReadOnlySpan<byte> ExtensionsProp
        => new[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e' ,
            (byte)'n' ,
            (byte)'s' ,
            (byte)'i' ,
            (byte)'o' ,
            (byte)'n',
            (byte)'s'
        };
}
