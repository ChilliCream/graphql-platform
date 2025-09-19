namespace HotChocolate.Internal;

/// <summary>
/// The type info provides information about the type structure that is relevant to
/// the GraphQL type system. A <see cref="ITypeInfo"/> can be created for runtime
/// types as well as for schema types.
/// </summary>
public interface ITypeInfo
{
    /// <summary>
    /// Gets the type component that represents the named type.
    /// </summary>
    Type NamedType { get; }

    /// <summary>
    /// Gets the original type from which this type info was inferred.
    /// </summary>
    Type OriginalType { get; }

    /// <summary>
    /// The components represent the GraphQL type structure.
    /// </summary>
    IReadOnlyList<TypeComponent> Components { get; }

    /// <summary>
    /// Defines if the <see cref="NamedType"/> is a GraphQL schema type.
    /// </summary>
    bool IsSchemaType { get; }

    /// <summary>
    /// Defines if the <see cref="NamedType"/> is a runtime type.
    /// </summary>
    bool IsRuntimeType { get; }

    /// <summary>
    /// If this type is a schema type then this method defines if it is an input type.
    /// </summary>
    bool IsInputType();

    /// <summary>
    /// If this type is a schema type then this method defines if it is an output type.
    /// </summary>
    bool IsOutputType();

    /// <summary>
    /// Gets the extended type that contains information
    /// about type arguments and nullability.
    /// </summary>
    IExtendedType GetExtendedType();
}
