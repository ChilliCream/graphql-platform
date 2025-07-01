using HotChocolate.Features;

namespace HotChocolate.Types;

/// <summary>
/// The base interface for GraphQL field definitions.
/// </summary>
public interface IFieldDefinition
    : INameProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , IDirectivesProvider
    , IFeatureProvider
    , ISyntaxNodeProvider
    , ISchemaCoordinateProvider
{
    /// <summary>
    /// Gets the type system member that declares this field definition.
    /// </summary>
    ITypeSystemMember DeclaringMember { get; }

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// Specifies if this field is part of the introspection.
    /// </summary>
    bool IsIntrospectionField => Name.StartsWith("__");

    /// <summary>
    /// Gets flags that describe additional properties of this field.
    /// </summary>
    FieldFlags Flags { get; }
}
