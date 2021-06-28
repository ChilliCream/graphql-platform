using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents a variable value.
    /// </summary>
    public readonly struct VariableValue
    {
        public VariableValue(NameString name, IInputType type, IValueNode value)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the variable name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the variable type.
        /// </summary>
        public IInputType Type { get; }

        /// <summary>
        /// Gets the variable value.
        /// </summary>
        public IValueNode Value { get; }
    }
}
