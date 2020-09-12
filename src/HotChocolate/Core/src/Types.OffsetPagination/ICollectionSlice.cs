using System.Collections.Generic;

namespace HotChocolate.Types.OffsetPaging
{
    public interface ICollectionSlice
    {
        IReadOnlyCollection<object> Nodes { get; }

        int TotalCount { get; }
    }
}
