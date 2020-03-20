using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserDeserializerMethod
        : ICodeDescriptor
    {
        public ResultParserDeserializerMethod(
            string name,
            string serializationType,
            string runtimeType,
            IReadOnlyList<ResultTypeDescriptor> runtimeTypeComponents,
            ValueSerializerDescriptor serializer)
        {
            Name = name;
            SerializationType = serializationType;
            RuntimeType = runtimeType;
            RuntimeTypeComponents = runtimeTypeComponents;
            Serializer = serializer;
        }

        public string Name { get; }

        public string SerializationType { get; }

        public string RuntimeType { get; }

        public IReadOnlyList<ResultTypeDescriptor> RuntimeTypeComponents { get; }

        public ValueSerializerDescriptor Serializer { get; }
    }
}
