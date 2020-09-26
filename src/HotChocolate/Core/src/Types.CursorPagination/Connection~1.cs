using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public class Connection<T> : Connection
    {
        public Connection(
            IReadOnlyCollection<Edge<T>> edges,
            ConnectionPageInfo info,
            Func<CancellationToken, ValueTask<int>> getTotalCount)
            : base(edges, info, getTotalCount)
        {
            Edges = edges;
        }

        public new IReadOnlyCollection<Edge<T>> Edges { get; }
    }
}
