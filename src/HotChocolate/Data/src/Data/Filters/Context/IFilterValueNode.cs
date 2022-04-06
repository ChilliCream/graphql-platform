using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a value of a filter. This value can either be a collection
/// (<see cref="IFilterValueCollection"/>) or a concrete value (<see cref="FilterInfo"/>)
/// </summary>
public interface IFilterValueNode
{
    /// <summary>
    /// The type of this value
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// The GraphQL value node of this filter
    /// </summary>
    IValueNode ValueNode { get; }
}
