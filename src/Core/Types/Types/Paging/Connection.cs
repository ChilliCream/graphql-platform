using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Paging
{
    public class Connection<T>
        : IConnection
    {
        public Connection(
            IPageInfo pageInfo,
            IReadOnlyCollection<Edge<T>> edges)
        {
            PageInfo = pageInfo
                ?? throw new ArgumentNullException(nameof(pageInfo));
            Edges = edges
                ?? throw new ArgumentNullException(nameof(edges));
        }

        public IPageInfo PageInfo { get; }

        public IReadOnlyCollection<Edge<T>> Edges { get; }

        IReadOnlyCollection<IEdge> IConnection.Edges => Edges;
    }
}
