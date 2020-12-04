using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            ResultTypeDescriptor resultType,
            IReadOnlyList<ResultTypeDescriptor> possibleTypes,
            bool isRoot)
        {
            Name = name;
            ResultType = resultType;
            PossibleTypes = possibleTypes;
            IsRoot = isRoot;
        }

        public string Name { get; }

        public ResultTypeDescriptor ResultType { get; }

        public IReadOnlyList<ResultTypeDescriptor> PossibleTypes { get; }

        public bool IsRoot { get; }
    }
}
