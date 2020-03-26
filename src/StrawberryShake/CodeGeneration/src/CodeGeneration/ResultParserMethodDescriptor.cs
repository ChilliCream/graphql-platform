using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            string resultType,
            IReadOnlyList<ResultTypeDescriptor> possibleTypes,
            bool isRoot)
        {
            Name = name;
            ResultType = resultType;
            PossibleTypes = possibleTypes;
            IsRoot = isRoot;
        }

        public string Name { get; }

        public string ResultType { get; }

        public IReadOnlyList<ResultTypeDescriptor> PossibleTypes { get; }

        public bool IsRoot { get; }
    }
}
