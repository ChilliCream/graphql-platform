using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a variable value.
/// </summary>
public readonly struct VariableValue(string name, IInputType type, IValueNode value)
{
    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name { get; } = name.EnsureGraphQLName();

    /// <summary>
    /// Gets the variable type.
    /// </summary>
    public IInputType Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    /// <summary>
    /// Gets the variable value.
    /// </summary>
    public IValueNode Value { get; } = value ?? throw new ArgumentNullException(nameof(value));
}
