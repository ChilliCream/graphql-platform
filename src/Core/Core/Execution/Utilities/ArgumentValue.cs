using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public readonly struct ArgumentValue
    {
        public ArgumentValue(IInputField argument, ValueKind kind, object value)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Kind = kind;
            Error = null;

            if (value is IValueNode literal)
            {
                Literal = literal;
                Value = null;
            }
            else
            {
                Value = value;
                Literal = null;
            }
        }

        public ArgumentValue(IInputField argument, IError error)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Kind = null;
            Value = null;
            Literal = null;
        }

        public IInputField Argument { get; }

        public IInputType Type => Argument.Type;

        public ValueKind? Kind { get; }

        public object Value { get; }

        public IValueNode Literal { get; }

        public IError Error { get; }
    }
}
