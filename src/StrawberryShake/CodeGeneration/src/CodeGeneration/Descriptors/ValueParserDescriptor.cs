using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    public struct ValueParserDescriptor
    {
        public ValueParserDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType,
            RuntimeTypeInfo serializedType)
        {
            Name = name;
            RuntimeType = runtimeType;
            SerializedType = serializedType;
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString Name { get; }

        public RuntimeTypeInfo RuntimeType { get; }

        public RuntimeTypeInfo SerializedType { get; }
    }
}
