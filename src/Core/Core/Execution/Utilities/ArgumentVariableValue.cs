using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ArgumentVariableValue
    {
        public ArgumentVariableValue(
            IInputType type,
            NameString variableName,
            object defaultValue,
            IFieldValueSerializer serializer)
        {
            Type = type;
            VariableName = variableName;
            DefaultValue = defaultValue;
            Serializer = serializer;
        }

        public IInputType Type { get; }

        public NameString VariableName { get; }

        public object DefaultValue { get; }

        public IFieldValueSerializer Serializer { get; }
    }
}
