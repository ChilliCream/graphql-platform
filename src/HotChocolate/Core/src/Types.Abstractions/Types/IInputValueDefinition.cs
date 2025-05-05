using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IInputValueDefinition : IFieldDefinition
{
    IValueNode? DefaultValue { get; }

    /// <summary>
    /// Creates an <see cref="InputValueDefinitionNode"/> from the current <see cref="IInputValueDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns an <see cref="InputValueDefinitionNode"/>.
    /// </returns>
    new InputValueDefinitionNode ToSyntaxNode();
}
