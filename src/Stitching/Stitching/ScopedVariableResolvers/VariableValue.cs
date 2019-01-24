using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    internal class VariableValue
    {
        internal VariableValue(
            string name, ITypeNode type,
            object value, IValueNode defaultValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public ITypeNode Type { get; }
        public object Value { get; }
        public IValueNode DefaultValue { get; }
    }
}
