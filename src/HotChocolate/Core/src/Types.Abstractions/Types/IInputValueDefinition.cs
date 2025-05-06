using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents a definition of a GraphQL input value.
/// </summary>
public interface IInputValueDefinition
    : IFieldDefinition
    , ISyntaxNodeProvider<InputValueDefinitionNode>
{
    /// <summary>
    /// Gets the default value of the input value.
    /// </summary>
    IValueNode? DefaultValue { get; }
}
