using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public readonly struct VariableValue
    {
        public VariableValue(
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
