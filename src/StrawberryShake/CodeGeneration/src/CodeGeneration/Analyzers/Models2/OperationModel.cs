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
            IReadOnlyList<INamedType> leafTypes)
        {
            Name = name;
            Type = type;
            Document = document;
            Operation = operation;
            Arguments = arguments;
            LeafTypes = leafTypes;
        }

        public string Name { get; }

        public ObjectType Type { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<ArgumentModel> Arguments { get; }

        /// <summary>
        /// Gets the leaf types that are used by this operation.
        /// </summary>
        public IReadOnlyList<INamedType> LeafTypes { get; }
    }
}
