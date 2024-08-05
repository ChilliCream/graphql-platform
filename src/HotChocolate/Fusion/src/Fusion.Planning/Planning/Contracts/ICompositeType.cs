using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a GraphQL type composed of multiple downstream types.
/// </summary>
public interface ICompositeType
{
    /// <summary>
    /// Gets the kind of the composite type.
    /// </summary>
    TypeKind Kind { get; }
}
