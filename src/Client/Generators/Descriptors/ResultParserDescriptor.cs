using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public class ResultParserDescriptor
        : IResultParserDescriptor
    {
        public ResultParserDescriptor(
            string name,
            OperationDefinitionNode operation,
            IReadOnlyList<IResultParserMethodDescriptor> parseMethods)
        {
            Name = name;
            Operation = operation;
            ParseMethods = parseMethods;
        }

        public string Name { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<IResultParserMethodDescriptor> ParseMethods { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            return ParseMethods;
        }
    }
}
