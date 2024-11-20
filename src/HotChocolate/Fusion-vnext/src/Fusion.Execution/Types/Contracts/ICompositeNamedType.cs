using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL named type composed of multiple downstream types.
/// </summary>
public interface ICompositeNamedType : ICompositeType
{
    /// <summary>
    /// Gets the name of the composite type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the composite type.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the directives of the composite type.
    /// </summary>
    DirectiveCollection Directives { get; }

    /// <summary>
    /// Defines if this type is assignable from the given <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// The type that shall be checked if it is assignable.
    /// </param>
    /// <returns>
    /// <c>true</c> if this type is assignable from the given <paramref name="type" />; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAssignableFrom(ICompositeNamedType type)
        => ReferenceEquals(type, this);
}
