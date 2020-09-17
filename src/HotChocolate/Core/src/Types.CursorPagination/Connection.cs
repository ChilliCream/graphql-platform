using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public class Connection : IPage
    {
        private readonly Func<CancellationToken, ValueTask<int>> _getTotalCount;

        public Connection(
            IReadOnlyCollection<IEdge> edges,
            ConnectionPageInfo info,
            Func<CancellationToken, ValueTask<int>> getTotalCount)
        {
            _getTotalCount = getTotalCount ??
                throw new ArgumentNullException(nameof(getTotalCount));
            Edges = edges ??
                throw new ArgumentNullException(nameof(edges));
            Info = info ?? 
                throw new ArgumentNullException(nameof(info));
        }

        public IReadOnlyCollection<IEdge> Edges { get; }

        IReadOnlyCollection<object> IPage.Items => Edges;

        public ConnectionPageInfo Info { get; }

        IPageInfo IPage.Info => Info;

        public ValueTask<int> GetTotalCountAsync(
            CancellationToken cancellationToken) =>
            _getTotalCount(cancellationToken);
    }
}
