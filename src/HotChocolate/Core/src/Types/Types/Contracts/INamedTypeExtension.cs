using HotChocolate.Configuration;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This is not a full type and is used to split the type configuration into multiple part.
/// Any type extension instance is will not survive the initialization and instead is
/// merged into the target type.
/// </summary>
public interface INamedTypeExtension
    : ITypeSystemMember
    , IHasName
{
    /// <summary>
    /// The kind of type this type extension extends.
    /// </summary>
    TypeKind Kind { get; }

    /// <summary>
    /// Gets a type which this type extension extends.
    /// The type can be null if a name is used to match this.
    /// The type can be a runtime type or a schema type and
    /// needs either to match fully the extended type or be
    /// implemented by it.
    /// </summary>
    Type? ExtendsType { get; }
}

/// <summary>
/// This internal interface is used by the type initialization to
/// merge the type extension into the actual type..
/// </summary>
internal interface INamedTypeExtensionMerger : INamedTypeExtension
{
    /// <summary>
    /// The merge method that allows to merge the type extension into the named type.
    /// </summary>
    /// <param name="context">The type extension completion context.</param>
    /// <param name="type">The target type into which we merge the type extension.</param>
    void Merge(ITypeCompletionContext context, INamedType type);
}
