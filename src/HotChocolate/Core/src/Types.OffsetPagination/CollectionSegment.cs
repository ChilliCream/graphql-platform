using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegment : IPage
    {
        public CollectionSegment(IReadOnlyCollection<object> items, CollectionSegmentInfo info)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public IReadOnlyCollection<object> Items { get; }

        public IPageInfo Info { get; }
    }
}
