using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal readonly struct ScopedVariableValue
    {
        internal ScopedVariableValue(
            string name,
            ITypeNode type,
            IValueNode? value,
            IValueNode? defaultValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public ITypeNode Type { get; }

        public IValueNode? Value { get; }

        public IValueNode? DefaultValue { get; }

        public ScopedVariableValue WithValue(IValueNode value) =>
            new ScopedVariableValue(Name, Type, value, DefaultValue);
    }
}
