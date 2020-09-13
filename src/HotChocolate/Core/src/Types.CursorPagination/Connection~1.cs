using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public class Connection<T> : Connection
    {
        public Connection(IReadOnlyCollection<Edge<T>> edges, ConnectionPageInfo info)
            : base(edges, info)
        {
            Edges = edges;
        }

        public new IReadOnlyCollection<Edge<T>> Edges { get; }
    }
}
