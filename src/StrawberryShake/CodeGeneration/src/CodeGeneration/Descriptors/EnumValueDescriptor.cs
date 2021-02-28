using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumValueDescriptor : ICodeDescriptor
    {
        public EnumValueDescriptor(
            string runtimeValue,
            string name,
            long? value = null)
        {
            RuntimeValue = runtimeValue;
            Name = name;
            Value = value;
        }

        public NameString RuntimeValue { get; }
        public  NameString Name { get; }

        public long? Value { get; }
    }
}
