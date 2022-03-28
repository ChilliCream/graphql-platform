using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of <see cref="IFilterValueInfo">
/// </summary>
public class FilterValueCollection : List<IFilterValueInfo>, IFilterValueCollection
{
    /// <summary>
    /// Creates a new instance of <see cref="FilterValueCollection"/>
    /// </summary>
    public FilterValueCollection(
        IType type,
        IValueNode valueNode,
        IEnumerable<IFilterValueInfo> collection)
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
