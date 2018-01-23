namespace Zeus.Abstractions
{
    public class ScalarValue
        : IValue
    {
        public ScalarValue(NamedType type, string value)
        {
            Type = type ?? throw new System.ArgumentNullException(nameof(type));
            Value = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        public IType Type { get; }
        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}