using System;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// Denotes a GraphQL connection entity.
    /// </summary>
    /// <remarks>
    /// Note that "connection" here refers to a "cursor connection" as defined by the
    /// [Relay Cursor Connections Sepecification](http://facebook.github.io/relay/graphql/connections.htm)
    /// rather than an <see cref="IConnection"/>.
    /// </remarks>
    public interface IPagingConnection : IQueryableValue
    {
        /// <summary>
        /// Gets the paging information field for the connection.
        /// </summary>
        IPageInfo PageInfo { get; }
    }

    /// <summary>
    /// Denotes a GraphQL connection entity.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    public interface IPagingConnection<TNode> : IPagingConnection
    {
        /// <summary>
        /// Gets the connection nodes.
        /// </summary>
        IQueryableList<TNode> Nodes { get; }
    }
}
