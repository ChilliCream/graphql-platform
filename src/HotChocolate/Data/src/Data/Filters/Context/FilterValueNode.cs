using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// An abstract class that represents the value of a filter
/// </summary>
public abstract class FilterValueNode : IFilterValueNode
{
    /// <summary>
    /// Creates a new instance of <see cref="FilterValueNode"/>
    /// </summary>
    protected FilterValueNode(IType type, IValueNode valueNode)
    {
        Type = type;
        ValueNode = valueNode;
    }

    /// <inheritdoc />
    public IType Type { get; }

    /// <inheritdoc />
    public IValueNode ValueNode { get; }
}
