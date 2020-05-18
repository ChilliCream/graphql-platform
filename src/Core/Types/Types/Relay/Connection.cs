using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Relay
{
    public class Connection<T>
        : IConnection
    {
        public Connection(
            IPageInfo pageInfo,
            IReadOnlyList<Edge<T>> edges)
        {
            PageInfo = pageInfo ?? throw new ArgumentNullException(nameof(pageInfo));
            Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        }

        public IPageInfo PageInfo { get; }

        public IReadOnlyList<Edge<T>> Edges { get; }

        IReadOnlyList<IEdge> IConnection.Edges => Edges;
    }
}
