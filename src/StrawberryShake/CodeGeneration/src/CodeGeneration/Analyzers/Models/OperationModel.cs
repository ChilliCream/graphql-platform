using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class OperationModel
    {
        public OperationModel(
            string name,
            ObjectType type,
            DocumentNode document,
            OperationDefinitionNode operation,
            ParserModel parser,
            IReadOnlyList<ArgumentModel> arguments)
        {
            Name = name;
            Type = type;
            Document = document;
            Operation = operation;
            Parser = parser;
            Arguments = arguments;
        }

        public string Name { get; }

        public ObjectType Type { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Operation { get; }

        public ParserModel Parser { get; }

        public IReadOnlyList<ArgumentModel> Arguments { get; }
    }
}
