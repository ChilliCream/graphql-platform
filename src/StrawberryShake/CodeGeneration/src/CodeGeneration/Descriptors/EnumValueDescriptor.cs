using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumValueDescriptor : ICodeDescriptor
    {
        public EnumValueDescriptor(string name, long? value = null)
        {
            Name = name;
            Value = value;
        }

        public NameString Name { get; }

        public long? Value { get; }
    }
}
