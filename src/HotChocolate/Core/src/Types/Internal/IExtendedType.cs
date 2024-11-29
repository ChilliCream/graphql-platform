#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Internal;

/// <summary>
/// The extended type provides addition type information about the underlying system type.
/// </summary>
public interface IExtendedType : IEquatable<IExtendedType>
{
    /// <summary>
    /// Gets the underlying type.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the original type representation.
    /// In case of a nullable int the original type will be
    /// <see cref="Nullable{T}"/>.
    /// </summary>
    Type Source { get; }

    /// <summary>
    /// Gets the generic type definition if this is a generic type.
    /// </summary>
    Type? Definition { get; }

    /// <summary>
    /// Specifies the extended type kind.
    /// </summary>
    ExtendedTypeKind Kind { get; }

    /// <summary>
    /// Defines that this type is a generic type.
    /// </summary>
    bool IsGeneric { get; }

    /// <summary>
    /// Defines that this type is a C# array.
    /// </summary>
    bool IsArray { get; }

    /// <summary>
    /// Defines that this type is a supported list type.
    /// </summary>
    bool IsList { get; }

    /// <summary>
    /// Defines if this is a collection type meaning it is either <see cref="IsArray"/> or
    /// <see cref="IsList"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ElementType))]
    bool IsArrayOrList { get; }

    /// <summary>
    /// Specifies that this type is a schema type and implements
    /// ScalarType, ObjectType, InterfaceType, EnumType, UnionType or
    /// InputObjectType.
    /// </summary>
    bool IsNamedType { get; }

    /// <summary>
    /// Specifies that this type is a schema type.
    /// </summary>
    bool IsSchemaType { get; }

    /// <summary>
    /// Specifies if this type is an interface.
    /// </summary>
    bool IsInterface { get; }

    /// <summary>
    /// Specifies if this type is nullable.
    /// </summary>
    bool IsNullable { get; }

    /// <summary>
    /// Gets the generic type information.
    /// </summary>
    IReadOnlyList<IExtendedType> TypeArguments { get; }

    /// <summary>
    /// Gets the element type if <see cref="IsArrayOrList"/> is <c>true</c>.
    /// </summary>
    IExtendedType? ElementType { get; }
}
