using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct VariableValue
    {
        public VariableValue(
            IInputType type,
            NameString variableName,
            object defaultValue)
        {
            Type = type;
            VariableName = variableName;
            DefaultValue = defaultValue;
        }

        public IInputType Type { get; }

        public NameString VariableName { get; }

        public object DefaultValue { get; }
    }
}
