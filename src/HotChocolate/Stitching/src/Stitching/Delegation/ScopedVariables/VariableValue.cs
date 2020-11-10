using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal readonly struct VariableValue
    {
        internal VariableValue(
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

        public VariableValue WithValue(IValueNode value) =>
            new VariableValue(Name, Type, value, DefaultValue);
    }
}
