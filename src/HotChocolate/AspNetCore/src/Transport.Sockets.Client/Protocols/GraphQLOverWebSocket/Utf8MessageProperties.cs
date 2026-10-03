#if FUSION
using HotChocolate.Fusion.Transport.Serialization;
#else
using HotChocolate.Transport.Serialization;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#endif

internal static class Utf8MessageProperties
{
    public static ReadOnlySpan<byte> IdProp => Utf8GraphQLRequestProperties.IdProp;

    public static ReadOnlySpan<byte> TypeProp => "type"u8;

    public static ReadOnlySpan<byte> PayloadProp => "payload"u8;

    public static ReadOnlySpan<byte> DataProp => Utf8GraphQLResultProperties.DataProp;

    public static ReadOnlySpan<byte> ErrorsProp => Utf8GraphQLResultProperties.ErrorsProp;

    public static ReadOnlySpan<byte> ExtensionsProp => Utf8GraphQLRequestProperties.ExtensionsProp;

    public static ReadOnlySpan<byte> RequestIndexProp => Utf8GraphQLResultProperties.RequestIndexProp;

    public static ReadOnlySpan<byte> VariableIndexProp => Utf8GraphQLResultProperties.VariableIndexProp;
}
