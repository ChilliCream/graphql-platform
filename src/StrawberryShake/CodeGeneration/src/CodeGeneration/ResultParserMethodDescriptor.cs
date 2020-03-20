using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
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
}
