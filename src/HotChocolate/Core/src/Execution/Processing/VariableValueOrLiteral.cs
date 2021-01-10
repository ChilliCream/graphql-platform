using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal readonly struct VariableValueOrLiteral
    {
        public VariableValueOrLiteral(
            IInputType type,
            object? value,
            IValueNode? valueLiteral)
        {
            Type = type;
            Value = value;
            ValueLiteral = valueLiteral;
        }

        public IInputType Type { get; }

        public object? Value { get; }

        public IValueNode? ValueLiteral { get; }
    }
}
