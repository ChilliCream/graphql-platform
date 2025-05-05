using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IScalarTypeDefinition : IOutputTypeDefinition, IInputTypeDefinition
{
    /// <summary>
    /// Creates a <see cref="ScalarTypeDefinitionNode"/> from the current <see cref="IScalarTypeDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="ScalarTypeDefinitionNode"/>.
    /// </returns>
    new ScalarTypeDefinitionNode ToSyntaxNode();
}
