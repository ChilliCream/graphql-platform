using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a variable value.
/// </summary>
public readonly struct VariableValue
{
    /// <summary>
    /// Initializes a new instance of <see cref="VariableValue"/>.
    /// </summary>
    /// <param name="name">
    /// The variable name.
    /// </param>
    /// <param name="type">
    /// The variable type.
    /// </param>
    /// <param name="value">
    /// The variable value.
    /// </param>
    public VariableValue(string name, IInputType type, IValueNode value)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);

        Name = name.EnsureGraphQLName();
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the variable type.
    /// </summary>
    public IInputType Type { get; }

    /// <summary>
    /// Gets the variable value.
    /// </summary>
    public IValueNode Value { get; }
}
