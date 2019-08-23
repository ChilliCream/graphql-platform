using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ResultParserMethodDescriptor
       : IResultParserMethodDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            IType resultType,
            FieldNode resultSelection,
            Path path,
            IInterfaceDescriptor resultDescriptor,
            IReadOnlyList<IResultParserTypeDescriptor> possibleTypes)
        {
            Name = name;
            ResultType = resultType;
            ResultSelection = resultSelection;
            Path = path;
            ResultDescriptor = resultDescriptor;
            PossibleTypes = possibleTypes;
        }

        public string Name { get; }

        public IType ResultType { get; }

        public FieldNode ResultSelection { get; }

        public Path Path { get; }

        public IInterfaceDescriptor ResultDescriptor { get; }

        public IReadOnlyList<IResultParserTypeDescriptor> PossibleTypes { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield return ResultDescriptor;

            foreach (IClassDescriptor possibleType in
                PossibleTypes.Select(t => t.ResultDescriptor))
            {
                yield return possibleType;
            }
        }
    }
}
