using System.Collections.Generic;

namespace HotChocolate.Types.OffsetPaging
{
    public class CollectionSlice<TClrType> : ICollectionSlice
    {
        public IReadOnlyCollection<TClrType> Nodes { get; }

        public int TotalCount { get; }

        IReadOnlyCollection<object> ICollectionSlice.Nodes { get { return (IReadOnlyCollection<object>)Nodes; } }

        public CollectionSlice(IReadOnlyCollection<TClrType> slice, int totalCount)
        {
            Nodes = slice;
            TotalCount = totalCount;
        }
    }
}