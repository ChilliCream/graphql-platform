using System.Collections.Generic;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a collection of <see cref="ISortingValueNode"/>
/// </summary>
public interface ISortingValueCollection
    : IEnumerable<ISortingValueNode>
    , ISortingValueNode
{
}
