using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ArgumentVariableValue
    {
        private readonly Func<IInputField, object, object> _coerceArgumentValue;

        public ArgumentVariableValue(
            IInputField argument,
            NameString variableName,
            object defaultValue,
            Func<IInputField, object, object> coerceArgumentValue)
        {
            Argument = argument;
            VariableName = variableName;
            DefaultValue = defaultValue;
            _coerceArgumentValue = coerceArgumentValue;
        }

        public IInputField Argument { get; }

        public IInputType Type => Argument.Type;

        public NameString VariableName { get; }

        public object DefaultValue { get; }

        public object CoerceValue(object value) =>
            _coerceArgumentValue(Argument, value);
    }
}
