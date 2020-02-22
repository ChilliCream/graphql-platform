using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string Namespace { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultParserMethodDescriptor> Methods { get; }

        public IReadOnlyList<ResultParserDeserializerMethod> DeserializerMethods { get; }

        public IReadOnlyList<ValueSerializerDescriptor> ValueSerializers { get; }
    }

    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }
    }

    public class ResultParserDeserializerMethod
        : ICodeDescriptor
    {
        public string Name { get; }
    }
}
