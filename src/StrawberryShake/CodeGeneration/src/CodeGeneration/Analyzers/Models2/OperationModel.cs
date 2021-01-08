using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    public class OperationModel
    {
        public OperationModel(
            string name,
            ObjectType type,
            DocumentNode document,
            OperationDefinitionNode operation,
            IReadOnlyList<ArgumentModel> arguments,
            OutputTypeModel resultType,
            IReadOnlyList<LeafTypeModel> leafTypes,
            IReadOnlyList<InputObjectTypeModel> inputObjectTypes)
        {
            Name = name;
            Type = type;
            Document = document;
            Operation = operation;
            Arguments = arguments;
            ResultType = resultType;
            LeafTypes = leafTypes;
            InputObjectTypes = inputObjectTypes;
        }

        public string Name { get; }

        public ObjectType Type { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<ArgumentModel> Arguments { get; }

        public OutputTypeModel ResultType { get; }

        public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

        public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }
    }
}
