using System.Collections.Generic;

namespace HotChocolate.Types.OffsetPaging
{
    public class CollectionSlice<TClrType>
        : ICollectionSlice
    {
        public CollectionSlice(IReadOnlyCollection<TClrType> slice, int totalCount)
        {
            Nodes = slice;
            TotalCount = totalCount;
        }
        
        public IReadOnlyCollection<TClrType> Nodes { get; }
        
        IReadOnlyCollection<object> ICollectionSlice.Nodes => (IReadOnlyCollection<object>)Nodes;

        public int TotalCount { get; }
    }
}
