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
            Error = null;
        }

        public ArgumentValue(IInputType type, IError error)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Value = null;
        }

        public IInputType Type { get; }

        public object Value { get; }

        public IError Error { get; }
    }
}
