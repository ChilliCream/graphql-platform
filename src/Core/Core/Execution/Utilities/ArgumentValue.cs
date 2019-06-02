using System;
using HotChocolate.Language;
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
            Literal = null;
        }

        public ArgumentValue(IInputType type, IError error)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Value = null;
            Literal = null;
        }

        public ArgumentValue(IInputType type, IValueNode literal)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Literal = literal
                ?? throw new ArgumentNullException(nameof(literal));
            Value = null;
            Error = null;
        }

        public IInputType Type { get; }

        public object Value { get; }

        public IValueNode Literal { get; }

        public IError Error { get; }
    }
}
