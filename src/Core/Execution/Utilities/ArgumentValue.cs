using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ArgumentValue
    {
        public ArgumentValue(IInputType type, Type clrType, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Type = type;
            ClrType = clrType;
            Value = value;
        }

        public IInputType Type { get; }

        public Type ClrType { get; }

        public object Value { get; }
    }
}
