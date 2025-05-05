using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IOutputFieldDefinition : IFieldDefinition
{
    new IComplexTypeDefinition DeclaringType { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from the current <see cref="IOutputFieldDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="FieldDefinitionNode"/>.
    /// </returns>
    new FieldDefinitionNode ToSyntaxNode();
}
