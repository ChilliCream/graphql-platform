namespace StrawberryShake.CodeGeneration
{
    public class InputTypeSerializerMethodDescriptor
        : ICodeDescriptor
    {
        public InputTypeSerializerMethodDescriptor(
            string name,
            bool isNullableType,
            bool isListSerializer,
            string? valueSerializerFieldName,
            string? serializerMethodName)
        {
            Name = name;
            IsNullableType = isNullableType;
            IsListSerializer = isListSerializer;
            ValueSerializerFieldName = valueSerializerFieldName;
            SerializerMethodName = serializerMethodName;
        }

        public string Name { get; }

        public bool IsNullableType { get; }

        public bool IsListSerializer { get; }

        public string? ValueSerializerFieldName { get; }

        public string? SerializerMethodName { get; }
    }
}
