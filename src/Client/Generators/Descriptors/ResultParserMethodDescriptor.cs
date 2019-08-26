using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class ResultParserMethodDescriptor
       : IResultParserMethodDescriptor
    {
        public ResultParserMethodDescriptor(
            string name,
            OperationDefinitionNode operation,
            IType resultType,
            FieldNode resultSelection,
            Path path,
            IInterfaceDescriptor resultDescriptor,
            IReadOnlyList<IResultParserTypeDescriptor> possibleTypes)
            : this(name, operation, resultType, resultSelection,
                path, resultDescriptor, possibleTypes, null)
        { }

        public ResultParserMethodDescriptor(
            string name,
            OperationDefinitionNode operation,
            IType resultType,
            FieldNode resultSelection,
            Path path,
            IInterfaceDescriptor resultDescriptor,
            IReadOnlyList<IResultParserTypeDescriptor> possibleTypes,
            IResultParserTypeDescriptor unknownType)
        {
            Name = name;
            Operation = operation;
            ResultType = resultType;
            ResultSelection = resultSelection;
            Path = path;
            ResultDescriptor = resultDescriptor;
            PossibleTypes = possibleTypes;
            UnknownType = unknownType;
        }

        public OperationDefinitionNode Operation { get; }

        public string Name { get; }

        public IType ResultType { get; }

        public FieldNode ResultSelection { get; }

        public Path Path { get; }

        public IInterfaceDescriptor ResultDescriptor { get; }

        public IReadOnlyList<IResultParserTypeDescriptor> PossibleTypes { get; }

        public IResultParserTypeDescriptor UnknownType { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield return ResultDescriptor;
            ;

            foreach (IClassDescriptor possibleType in
                PossibleTypes.Select(t => t.ResultDescriptor))
            {
                yield return possibleType;
            }
        }
    }
}
