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
            bool isRoot)
        {
            Name = name;
            ResultType = resultType;
            ResultTypeComponents = resultTypeComponents;
            IsRoot = isRoot;
        }

        public string Name { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultTypeDescriptor> ResultTypeComponents { get; }

        public bool IsRoot { get; }
    }

    public class ResultParserPossibleTypeDescriptor
        : ICodeDescriptor
    {
        public string Name => throw new System.NotImplementedException();

        public IReadOnlyList<ResultTypeDescriptor> ResultTypeComponents { get; }
    }
}
