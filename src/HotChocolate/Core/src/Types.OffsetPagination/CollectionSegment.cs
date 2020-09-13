using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegment : IPage
    {
        private readonly Func<CancellationToken, ValueTask<int>> _getTotalCount;

        public CollectionSegment(
            IReadOnlyCollection<object> items,
            CollectionSegmentInfo info,
            Func<CancellationToken, ValueTask<int>> getTotalCount)
        {
            Items = items ??
                throw new ArgumentNullException(nameof(items));
            Info = info;
            _getTotalCount = getTotalCount ??
                throw new ArgumentNullException(nameof(getTotalCount));
        }

        public IReadOnlyCollection<object> Items { get; }

        public IPageInfo Info { get; }

        public ValueTask<int> GetTotalCountAsync(
            CancellationToken cancellationToken) =>
            _getTotalCount(cancellationToken);
    }
}
