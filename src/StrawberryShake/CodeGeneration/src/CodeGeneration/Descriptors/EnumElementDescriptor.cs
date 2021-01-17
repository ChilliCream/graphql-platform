namespace StrawberryShake.CodeGeneration
{
    public class EnumElementDescriptor
        : ICodeDescriptor
    {
        public EnumElementDescriptor(string name, long? value = null)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public long? Value { get; }
    }
}
