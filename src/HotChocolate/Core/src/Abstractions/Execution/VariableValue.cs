using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a variable value.
/// </summary>
public readonly struct VariableValue
{
    public VariableValue(string name, IInputType type, IValueNode value)
    {
        Name = name.EnsureGraphQLName();
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value ?? throw new ArgumentNullException(nameof(value));
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
