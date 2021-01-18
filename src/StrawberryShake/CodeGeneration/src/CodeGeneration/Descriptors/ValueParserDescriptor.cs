namespace StrawberryShake.CodeGeneration
{
    public struct ValueParserDescriptor
    {
        public ValueParserDescriptor(
            string serializedType,
            string runtimeType,
            string graphQLTypeName)
        {
            SerializedType = serializedType;
            RuntimeType = runtimeType;
            GraphQLTypeName = graphQLTypeName;
        }

        public string SerializedType { get; }

        public string RuntimeType { get; }
        
        public string GraphQLTypeName { get; }
    }
}
