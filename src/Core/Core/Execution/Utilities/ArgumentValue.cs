using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public readonly struct ArgumentValue
    {
        public ArgumentValue(
            IInputType type,
            ValueKind kind,
            object value,
            IFieldValueSerializer serializer)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Kind = kind;
            Value = value;
            Serializer = serializer;
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
            Serializer = null;
        }

        public ArgumentValue(
            IInputType type,
            ValueKind kind,
            IValueNode literal,
            IFieldValueSerializer serializer)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Literal = literal ?? throw new ArgumentNullException(nameof(literal));
            Kind = kind;
            Serializer = serializer;
            Value = null;
            Error = null;
        }

        public IInputType Type { get; }

        public ValueKind? Kind { get; }

        public object Value { get; }

        public IValueNode Literal { get; }

        public IError Error { get; }

        public IFieldValueSerializer Serializer { get; }
    }
}
