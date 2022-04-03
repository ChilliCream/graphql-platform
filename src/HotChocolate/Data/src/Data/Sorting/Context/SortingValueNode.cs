using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// An abstract class that represents the value of a sorting
/// </summary>
public abstract class SortingValueNode : ISortingValueNode
{
    /// <summary>
    /// Creates a new instance of <see cref="SortingValueNode"/>
    /// </summary>
    protected SortingValueNode(IType type, IValueNode valueNode)
    {
        Type = type;
        ValueNode = valueNode;
    }

    /// <inheritdoc />
    public IType Type { get; }

    /// <inheritdoc />
    public IValueNode ValueNode { get; }
}
