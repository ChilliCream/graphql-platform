using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageProperties
{
    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> IdProp => Utf8GraphQLRequestProperties.IdProp;

    public static ReadOnlySpan<byte> TypeProp => "type"u8;

    public static ReadOnlySpan<byte> PayloadProp => "payload"u8;

    public static ReadOnlySpan<byte> DataProp => Utf8GraphQLResultProperties.DataProp;

    public static ReadOnlySpan<byte> ErrorsProp => Utf8GraphQLResultProperties.ErrorsProp;

    public static ReadOnlySpan<byte> ExtensionsProp => Utf8GraphQLRequestProperties.ExtensionsProp;
}
