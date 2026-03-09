using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class Utf8MessageProperties
{
    public static ReadOnlySpan<byte> IdProp => Utf8GraphQLRequestProperties.IdProp;

    public static ReadOnlySpan<byte> TypeProp => "type"u8;

    public static ReadOnlySpan<byte> PayloadProp => "payload"u8;

    public static ReadOnlySpan<byte> DataProp => Utf8GraphQLResultProperties.DataProp;

    public static ReadOnlySpan<byte> ErrorsProp => Utf8GraphQLResultProperties.ErrorsProp;

    public static ReadOnlySpan<byte> ExtensionsProp => Utf8GraphQLRequestProperties.ExtensionsProp;
}
