using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IOutputFieldDefinition : IFieldDefinition
{
    /// <summary>
    /// Gets the type definition that declares this output field definition.
    /// </summary>
    IComplexTypeDefinition DeclaringType { get; }

    /// <summary>
    /// Gets the field arguments of this output field definition.
    /// </summary>
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    new IOutputType Type { get; }

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from the current <see cref="IOutputFieldDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="FieldDefinitionNode"/>.
    /// </returns>
    new FieldDefinitionNode ToSyntaxNode();
}
