namespace StrawberryShake.CodeGeneration
{
    public class EnumElementDescriptor
        : ICodeDescriptor
    {
        public EnumElementDescriptor(string name, string serializedName, long? value = null)
        {
            Name = name;
            SerializedName = serializedName;
            Value = value;
        }

        public string Name { get; }

        public string SerializedName { get; }

        public long? Value { get; }
    }
}
