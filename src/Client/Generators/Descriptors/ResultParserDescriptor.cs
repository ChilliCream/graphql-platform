using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class ResultParserDescriptor
        : IResultParserDescriptor
    {
        public ResultParserDescriptor(
            string name,
            OperationDefinitionNode operation,
            ICodeDescriptor resultDescriptor,
            IReadOnlyList<IResultParserMethodDescriptor> parseMethods)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            ResultDescriptor = resultDescriptor
                ?? throw new ArgumentNullException(nameof(resultDescriptor));
            ParseMethods = parseMethods
                ?? throw new ArgumentNullException(nameof(parseMethods));

            InvolvedLeafTypes = CollectLeafTypes(parseMethods);
        }

        public string Name { get; }

        public OperationDefinitionNode Operation { get; }

        public ICodeDescriptor ResultDescriptor { get; }

        public IReadOnlyList<IResultParserMethodDescriptor> ParseMethods { get; }

        public IReadOnlyList<INamedType> InvolvedLeafTypes { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            return ParseMethods;
        }

        private static IReadOnlyList<INamedType> CollectLeafTypes(
            IEnumerable<IResultParserMethodDescriptor> parseMethods)
        {
            var leafTypes = new Dictionary<string, INamedType>();

            foreach (IResultParserMethodDescriptor method in parseMethods)
            {
                foreach (IClassDescriptor possibleType in
                    method.PossibleTypes.Select(t => t.ResultDescriptor))
                {
                    foreach (IOutputField field in
                        possibleType.Fields.Select(t => t.Field))
                    {
                        if (field.Type.IsLeafType())
                        {
                            INamedType namedType = field.Type.NamedType();
                            if (!leafTypes.ContainsKey(namedType.Name))
                            {
                                leafTypes.Add(namedType.Name, namedType);
                            }
                        }
                    }
                }
            }

            return leafTypes.Values.ToList();
        }
    }
}
