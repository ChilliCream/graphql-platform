using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of <see cref="IFilterValueNode"/>
/// </summary>
public class FilterValueCollection : List<IFilterValueNode>, IFilterValueCollection
{
    /// <summary>
    /// Creates a new instance of <see cref="FilterValueCollection"/>
    /// </summary>
    public FilterValueCollection(
        IType type,
        IValueNode valueNode,
        IEnumerable<IFilterValueNode> collection)
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
