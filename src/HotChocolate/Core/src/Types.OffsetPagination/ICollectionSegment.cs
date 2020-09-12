using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public interface ICollectionSegment
    {
        IReadOnlyCollection<object> Nodes { get; }
    }
}
