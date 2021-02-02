using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumValueDescriptor : ICodeDescriptor
    {
        public EnumValueDescriptor(
            string name,
            string graphQLName,
            long? value = null)
        {
            Name = name;
            GraphQLName = graphQLName;
            Value = value;
        }

        public NameString Name { get; }
        public  NameString GraphQLName { get; }

        public long? Value { get; }
    }
}
