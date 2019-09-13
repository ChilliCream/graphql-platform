using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public readonly struct ArgumentValue
    {
        public ArgumentValue(IInputField argument, object value)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Value = value;
            Error = null;
            Literal = null;
        }

        public ArgumentValue(IInputField argument, IError error)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Value = null;
            Literal = null;
        }

        public ArgumentValue(IInputField argument, IValueNode literal)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            Literal = literal
                ?? throw new ArgumentNullException(nameof(literal));
            Value = null;
            Error = null;
        }

        public IInputField Argument { get; }

        public IInputType Type => Argument.Type;

        public object Value { get; }

        public IValueNode Literal { get; }

        public IError Error { get; }
    }
}
