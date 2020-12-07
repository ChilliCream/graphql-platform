using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultParserMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            TypeClassDescriptor typeClass,
            IReadOnlyList<TypeClassDescriptor> possibleTypes,
            bool isRoot)
        {
            Name = name;
            TypeClass = typeClass;
            PossibleTypes = possibleTypes;
            IsRoot = isRoot;
        }

        public string Name { get; }

        public TypeClassDescriptor TypeClass { get; }

        public IReadOnlyList<TypeClassDescriptor> PossibleTypes { get; }

        public bool IsRoot { get; }
    }
}
