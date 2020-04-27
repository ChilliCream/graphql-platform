using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public class PreparedArgument
    {
        public PreparedArgument(
            IInputField argument,
            ValueKind kind,
            bool isFinal,
            object? value,
            IValueNode? valueLiteral)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Kind = kind;
            IsFinal = isFinal;
            Error = null;
            Value = value;
            ValueLiteral = valueLiteral;
        }

        public PreparedArgument(IInputField argument, IError error)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Kind = null;
            Value = null;
            ValueLiteral = null;
        }

        public IInputField Argument { get; }

        public IInputType Type => Argument.Type;

        public ValueKind? Kind { get; }

        public bool IsFinal { get; }

        public object? Value { get; }

        public IValueNode? ValueLiteral { get; }

        public IError? Error { get; }
    }
}
