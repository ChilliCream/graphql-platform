using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ArgumentValue
    {
        public ArgumentValue(IInputType type, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Value = value;
        }

        public IInputType Type { get; }

        public object Value { get; }
    }
}
