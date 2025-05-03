namespace HotChocolate.Types;

/// <summary>
/// The base interface for GraphQL field definitions.
/// </summary>
public interface IFieldDefinition
    : INameProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , IDirectivesProvider
    , ISyntaxNodeProvider
{
    /// <summary>
    /// Gets the type system member that declares this field definition.
    /// </summary>
    ITypeSystemMember DeclaringMember { get; }

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    /// <value></value>
    IType Type { get; }
}
