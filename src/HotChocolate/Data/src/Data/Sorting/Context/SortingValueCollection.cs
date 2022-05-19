using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a collection of <see cref="ISortingValueNode"/>
/// </summary>
public class SortingValueCollection : List<ISortingValueNode>, ISortingValueCollection
{
    /// <summary>
    /// Creates a new instance of <see cref="SortingValueCollection"/>
    /// </summary>
    public SortingValueCollection(
        IType type,
        IValueNode valueNode,
        IEnumerable<ISortingValueNode> collection)
        : base(collection)
    {
        Type = type;
        ValueNode = valueNode;
    }

    /// <inheritdoc />
    public IType Type { get; }

    /// <inheritdoc />
    public IValueNode ValueNode { get; }
}
