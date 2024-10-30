using HotChocolate.Transport.Sockets.Client.Properties;

namespace HotChocolate.Transport.Sockets.Client;

internal static class ThrowHelper
{
    public static Exception MessageHasNoId() =>
        new InvalidOperationException(SocketClientResources.GraphQLOverWebsockets_MessageHasNoId);
}
