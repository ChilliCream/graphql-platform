namespace StrawberryShake
{
    public readonly struct VariableValue
    {
        public VariableValue(string name, string typeName, object? value)
        {
            Name = name;
            TypeName = typeName;
            Value = value;
        }

        public string Name { get; }

        public string TypeName { get; }

        public object? Value { get; }
    }
}
