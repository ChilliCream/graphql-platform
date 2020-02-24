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
            IReadOnlyList<ResultParserDeserializerMethod> deserializerMethods,
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

        public IReadOnlyList<ResultParserDeserializerMethod> DeserializerMethods { get; }

        public IReadOnlyList<ValueSerializerDescriptor> ValueSerializers { get; }
    }

    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            string resultType,
            IReadOnlyList<ResultTypeDescriptor> resultTypeComponents,
            bool isRoot,
            IReadOnlyList<ResultFieldDescriptor> fields)
        {
            Name = name;
            ResultType = resultType;
            ResultTypeComponents = resultTypeComponents;
            IsRoot = isRoot;
            Fields = fields;
        }

        public string Name { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultTypeDescriptor> ResultTypeComponents { get; }

        public bool IsRoot { get; }

        public IReadOnlyList<ResultFieldDescriptor> Fields { get; }
    }

    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public ResultTypeDescriptor(
            string name,
            bool isNullable,
            bool isList,
            bool isReferenceType)
        {
            Name = name;
            IsNullable = isNullable;
            IsList = isList;
            IsReferenceType = isReferenceType;
        }

        public string Name { get; }

        public bool IsNullable { get; }

        public bool IsList { get; }

        public bool IsReferenceType { get; }
    }

    public class ResultFieldDescriptor
        : ICodeDescriptor
    {
        public ResultFieldDescriptor(
            string name,
            string parserMethodName)
        {
            Name = name;
            ParserMethodName = parserMethodName;
        }

        public string Name { get; }

        public string ParserMethodName { get; }
    }

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
