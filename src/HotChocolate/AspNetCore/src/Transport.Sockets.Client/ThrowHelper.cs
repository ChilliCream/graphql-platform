using HotChocolate.Transport.Sockets.Client.Properties;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

internal static class ThrowHelper
{
#if FUSION
    public static NotSupportedException FusionPayloadMaterializationNotSupported()
        => new("Fusion WebSocket payload materialization is not available.");
#endif

    public static Exception MessageHasNoId() =>
        new InvalidOperationException(SocketClientResources.GraphQLOverWebsockets_MessageHasNoId);
}
