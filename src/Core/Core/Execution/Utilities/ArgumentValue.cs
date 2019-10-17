using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public readonly struct ArgumentValue
    {
        public ArgumentValue(IInputType type, ValueKind kind, object value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
            Kind = kind;
            Error = null;
            Literal = null;
        }

        public ArgumentValue(IInputType type, IError error)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Kind = null;
            Value = null;
            Literal = null;
        }

        public ArgumentValue(IInputType type, ValueKind kind, IValueNode literal)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Literal = literal
                ?? throw new ArgumentNullException(nameof(literal));
            Kind = kind;
            Value = null;
            Error = null;
        }

        public IInputType Type { get; }

        public ValueKind? Kind { get; }

        public object Value { get; }

        public IValueNode Literal { get; }

        public IError Error { get; }
    }
}
