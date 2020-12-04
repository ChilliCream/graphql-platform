namespace StrawberryShake.CodeGeneration
{
    public class InputFieldSerializerDescriptor
        : ICodeDescriptor
    {
        public InputFieldSerializerDescriptor(
            string name,
            string graphQLFieldName,
            string serializerMethodName)
        {
            Name = name;
            GraphQLFieldName = graphQLFieldName;
            SerializerMethodName = serializerMethodName;
        }

        public string Name { get; }

        public string GraphQLFieldName { get; }

        /// <summary>
        /// Gets the serializer method name
        /// </summary>
        /// <value></value>
        public string SerializerMethodName { get; }
    }
}
