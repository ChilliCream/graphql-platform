using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumValueDescriptor : ICodeDescriptor
    {
        public EnumValueDescriptor(
            string runtimeValue,
            string name,
            string? documentation,
            long? value = null)
        {
            RuntimeValue = runtimeValue;
            Name = name;
            Documentation = documentation;
            Value = value;
        }

        public NameString RuntimeValue { get; }

        public  NameString Name { get; }

        public string? Documentation { get; }

        public long? Value { get; }
    }
}
