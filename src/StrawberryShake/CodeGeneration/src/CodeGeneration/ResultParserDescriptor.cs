using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string Namespace { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultParserMethodDescriptor> ParseMethods { get; }

        public IReadOnlyList<ResultParserDeserializerMethod> DeserializerMethods { get; }

        public IReadOnlyList<ValueSerializerDescriptor> ValueSerializers { get; }
    }

    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string ResultTypeInterface { get; }

        public IReadOnlyList<ResultTypeDescriptor> ResultType { get; }

        public bool IsRoot { get; }

        public IReadOnlyList<ResultFieldDescriptor> Fields { get; }
    }

    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public bool IsNullable { get; }

        public bool IsList { get; }

        public bool IsReferenceType { get; }
    }

    public class ResultFieldDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string ParserMethodName { get; }
    }

    public class ResultParserDeserializerMethod
        : ICodeDescriptor
    {
        public string Name { get; }
    }
}
