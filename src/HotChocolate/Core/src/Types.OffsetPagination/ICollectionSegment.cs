using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public interface ICollectionSegment
    {
        IReadOnlyCollection<object> Nodes { get; }

        /// <summary>
        /// Provides information about the 
        /// </summary>
        ICollectionSegmentInfo SegmentInfo { get; }
    }
}
