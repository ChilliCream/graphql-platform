using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Specifies the kind of type reference.
/// </summary>
public enum TypeReferenceKind
{
    /// <summary>
    /// The type reference is represented by a .NET type.
    /// </summary>
    ExtendedType = 0,

    /// <summary>
    /// The type reference is represented by a schema type reference.
    /// </summary>
    SchemaType = 1,

    /// <summary>
    /// The type reference is represented by a <see cref="ITypeNode"/>.
    /// </summary>
    Syntax = 2,

    /// <summary>
    /// The type reference is represented by a <see cref="ITypeNode"/> and
    /// contains a factory to create the type.
    /// </summary>
    Factory = 3,

    /// <summary>
    /// The type reference refers to a type that is dependant on another type.
    /// </summary>
    DependantFactory = 4
}
