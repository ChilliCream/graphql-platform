using Microsoft.AspNetCore.Connections;

namespace HotChocolate.AspNetCore.Subscriptions;

internal static class ProtocolNames
{
    // ReSharper disable InconsistentNaming
    public const string GraphQL_Transport_WS = "graphql-transport-ws";
    public const string GraphQL_WS = "graphql-ws";
    // ReSharper restore InconsistentNaming
}
