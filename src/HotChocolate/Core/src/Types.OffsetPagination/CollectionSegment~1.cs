using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegment<TRuntimeType> : ICollectionSegment
    {
        public CollectionSegment(IReadOnlyCollection<TRuntimeType> slice, int totalCount)
        {
            Nodes = slice;
            TotalCount = totalCount;
        }

        public IReadOnlyCollection<TRuntimeType> Nodes { get; }

        IReadOnlyCollection<object> ICollectionSegment.Nodes =>
            (IReadOnlyCollection<object>)Nodes;

        public int TotalCount { get; }
    }
}
