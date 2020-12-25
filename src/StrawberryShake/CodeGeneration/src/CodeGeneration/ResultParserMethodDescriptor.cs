using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            TypeDescriptor type,
            IReadOnlyList<TypeDescriptor> possibleTypes,
            bool isRoot)
        {
            Name = name;
            Type = type;
            PossibleTypes = possibleTypes;
            IsRoot = isRoot;
        }

        public string Name { get; }

        public TypeDescriptor Type { get; }

        public IReadOnlyList<TypeDescriptor> PossibleTypes { get; }

        public bool IsRoot { get; }
    }
}
