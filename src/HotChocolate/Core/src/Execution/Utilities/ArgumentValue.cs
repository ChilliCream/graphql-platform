using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public sealed class ArgumentValue
    {
        public ArgumentValue(
            IInputField argument,
            ValueKind kind,
            bool isFinal,
            bool isImplicit,
            object? value,
            IValueNode valueLiteral)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Kind = kind;
            IsFinal = isFinal;
            IsImplicit = isImplicit;
            HasError = false;
            Error = null;
            Value = value;
            ValueLiteral = valueLiteral ?? throw new ArgumentNullException(nameof(valueLiteral));
        }

        public ArgumentValue(IInputField argument, IError error)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            HasError = true;
            IsFinal = true;
            Kind = null;
            Value = null;
            ValueLiteral = null;
        }

        public IInputField Argument { get; }

        public IInputType Type => Argument.Type;

        public IInputValueFormatter? Formatter => Argument.Formatter;

        public ValueKind? Kind { get; }

        public bool IsFinal { get; }

        public bool HasError { get; }

        public bool IsImplicit { get; }

        public object? Value { get; }

        public IValueNode? ValueLiteral { get; }

        public IError? Error { get; }
    }
}
