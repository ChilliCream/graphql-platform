using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class ParserModel
    {
        public ParserModel(
            string name,
            OperationDefinitionNode operation,
            ComplexOutputTypeModel returnType,
            IReadOnlyList<FieldParserModel> fieldParsers)
        {
            Name = name;
            Operation = operation;
            ReturnType = returnType;
            FieldParsers = fieldParsers;

            var leafTypes = new List<ILeafType>();

            foreach (FieldParserModel parser in fieldParsers)
            {
                foreach (ComplexOutputTypeModel model in parser.PossibleTypes)
                {
                    foreach (OutputFieldModel field in model.Fields)
                    {
                        if (field.Type.NamedType() is ILeafType leafType)
                        {
                            leafTypes.Add(leafType);
                        }
                    }
                }
            }

            LeafTypes = leafTypes;
        }

        public string Name { get; }

        /// <summary>
        /// Gets the operation that this parser handles.
        /// </summary>
        public OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the operation return type.
        /// </summary>
        public ComplexOutputTypeModel ReturnType { get; }

        /// <summary>
        /// Gets the leaf types that are used in this parser.
        /// </summary>
        public IReadOnlyList<INamedType> LeafTypes { get; }

        /// <summary>
        /// Gets the field parser models.
        /// </summary>
        public IReadOnlyList<FieldParserModel> FieldParsers { get; }
    }
}
