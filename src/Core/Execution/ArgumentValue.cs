using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ArgumentValue
    {
        public ArgumentValue(IInputType type, Type nativeType, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nativeType == null)
            {
                throw new ArgumentNullException(nameof(nativeType));
            }

            Type = type;
            NativeType = nativeType;
            Value = value;
        }

        public IInputType Type { get; }

        public Type NativeType { get; }

        public object Value { get; }
    }
}
