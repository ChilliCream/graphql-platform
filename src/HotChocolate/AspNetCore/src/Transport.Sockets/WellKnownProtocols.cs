namespace HotChocolate.Transport.Sockets;

/// <summary>
/// Constants for the supported websocket protocols.
/// </summary>
public static class WellKnownProtocols
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// The sub-protocol name for the GraphQL over WebSocket Protocol
    /// https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md
    /// </summary>
    public const string GraphQL_Transport_WS = "graphql-transport-ws";

    /// <summary>
    /// The sub-protocol name for the GraphQL over WebSocket Protocol from Apollo
    /// https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md
    /// see constants:
    /// https://github.com/apollographql/subscriptions-transport-ws/blob/master/src/protocol.ts
    /// </summary>
    public const string GraphQL_WS = "graphql-ws";
    // ReSharper restore InconsistentNaming
}
