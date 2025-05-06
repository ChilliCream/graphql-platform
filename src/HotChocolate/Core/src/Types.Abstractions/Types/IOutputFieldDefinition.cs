using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IOutputFieldDefinition : IFieldDefinition
{
    /// <summary>
    /// Gets the type definition that declares this field definition.
    /// </summary>
    new IComplexTypeDefinition DeclaringMember { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from the current <see cref="IOutputFieldDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="FieldDefinitionNode"/>.
    /// </returns>
    new FieldDefinitionNode ToSyntaxNode();

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    new IOutputType Type { get; }
}
