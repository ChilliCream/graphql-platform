using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public struct ValueParserDescriptor
    {
        public ValueParserDescriptor(
            string serializedType,
            string runtimeType,
            NameString graphQLTypeName)
        {
            SerializedType = serializedType;
            RuntimeType = runtimeType;
            GraphQLTypeName = graphQLTypeName;
        }

        public string SerializedType { get; }

        public string RuntimeType { get; }

        public NameString GraphQLTypeName { get; }
    }
}
