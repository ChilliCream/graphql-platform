using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public readonly struct ArgumentValue
    {
        public ArgumentValue(IInputType type, object value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
        }

        public IInputType Type { get; }

        public object Value { get; }
    }
}
