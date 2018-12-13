using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public class QueryablePageInfo
        : IPageInfo
    {
        private readonly Func<bool> _hasNextPage;
        private readonly Func<bool> _hasPreviousPage;

        public QueryablePageInfo(
            Func<bool> hasNextPage,
            Func<bool> hasPreviousPage)
        {
            _hasNextPage = hasNextPage;
            _hasPreviousPage = hasPreviousPage;
        }

        public Task<bool> HasNextPageAsync(CancellationToken cancellationToken)
        {
            return Task.Run(_hasNextPage, cancellationToken);
        }

        public Task<bool> HasPreviousAsync(CancellationToken cancellationToken)
        {
            return Task.Run(_hasPreviousPage, cancellationToken);
        }
    }
}
