using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public class PreparedArgument
    {
        public PreparedArgument(
            Argument argument,
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
            IsError = false;
            Error = null;
            Value = value;
            ValueLiteral = valueLiteral ?? throw new ArgumentNullException(nameof(valueLiteral));
        }

        public PreparedArgument(Argument argument, IError error)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            IsError = true;
            IsFinal = true;
            Kind = null;
            Value = null;
            ValueLiteral = null;
        }

        public Argument Argument { get; }

        public IInputType Type => Argument.Type;

        public ValueKind? Kind { get; }

        public bool IsFinal { get; }

        public bool IsError { get; }

        public bool IsImplicit { get; }

        public object? Value { get; }

        public IValueNode? ValueLiteral { get; }

        public IError? Error { get; }
    }
}
