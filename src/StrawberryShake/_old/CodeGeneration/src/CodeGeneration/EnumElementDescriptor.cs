namespace StrawberryShake.CodeGeneration
{
    public class EnumElementDescriptor
        : ICodeDescriptor
    {
        public EnumElementDescriptor(string name, string serializedName, int? value = null)
        {
            Name = name;
            SerializedName = serializedName;
            Value = value;
        }

        public string Name { get; }

        public string SerializedName { get; }

        public int? Value { get; }
    }
}
