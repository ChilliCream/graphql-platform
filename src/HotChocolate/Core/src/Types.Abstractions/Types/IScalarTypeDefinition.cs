using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IScalarTypeDefinition
    : IOutputTypeDefinition
    , IInputTypeDefinition
    , ISyntaxNodeProvider<ScalarTypeDefinitionNode>
{
    Uri? SpecifiedBy { get; }

    /// <summary>
    /// Checks if the value is an instance of this type.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <c>true</c> if the value is an instance of this type; otherwise, <c>false</c>.
    /// </returns>
    bool IsInstanceOfType(IValueNode value);
}
