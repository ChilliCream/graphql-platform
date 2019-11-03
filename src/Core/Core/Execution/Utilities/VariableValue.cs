using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class VariableValue
    {
        public VariableValue(
            NameString name,
            IInputType type,
            object value)
        {
            Name = name;
            Type = type;
            if (value is IValueNode literal)
            {
                Literal = literal;
            }
            else
            {
                Value = value;
            }
        }

        public NameString Name { get; }

        public IInputType Type { get; }

        public object Value { get; }

        public IValueNode Literal { get; }
    }
}
