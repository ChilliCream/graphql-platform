using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a value of a sorting. This value can either be a collection
/// (<see cref="ISortingValueCollection"/>) or a concrete value (<see cref="SortingInfo"/>)
/// </summary>
public interface ISortingValueNode
{
    /// <summary>
    /// The type of this value
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// The GraphQL value node of this sorting
    /// </summary>
    IValueNode ValueNode { get; }
}
