using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserDescriptor
        : ICodeDescriptor
    {
        public ResultParserDescriptor(
            string name,
            string @namespace,
            string resultType,
            IReadOnlyList<ResultParserMethodDescriptor> parseMethods,
            IReadOnlyList<ResultParserDeserializerMethodDescriptor> deserializerMethods,
            IReadOnlyList<ValueSerializerDescriptor> valueSerializers)
        {
            Name = name;
            Namespace = @namespace;
            ResultType = resultType;
            ParseMethods = parseMethods;
            DeserializerMethods = deserializerMethods;
            ValueSerializers = valueSerializers;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultParserMethodDescriptor> ParseMethods { get; }

        public IReadOnlyList<ResultParserDeserializerMethodDescriptor> DeserializerMethods { get; }

        public IReadOnlyList<ValueSerializerDescriptor> ValueSerializers { get; }
    }
}
