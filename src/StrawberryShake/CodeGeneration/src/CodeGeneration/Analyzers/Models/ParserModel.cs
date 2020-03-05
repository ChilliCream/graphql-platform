using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class ParserModel
    {
        public ParserModel(
            OperationDefinitionNode operation,
            ComplexInputTypeModel returnType,
            IReadOnlyList<INamedType> leafTypes,
            IReadOnlyList<FieldParserModel> fields)
        {
            Operation = operation;
            ReturnType = returnType;
            LeafTypes = leafTypes;
            Fields = fields;
        }

        /// <summary>
        /// Gets the operation that this parser handles.
        /// </summary>
        public OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the operation return type.
        /// </summary>
        public ComplexInputTypeModel ReturnType { get; }

        /// <summary>
        /// Gets the leaf types that are used in this parser.
        /// </summary>
        public IReadOnlyList<INamedType> LeafTypes { get; }

        /// <summary>
        /// Gets the field parser models.
        /// </summary>
        public IReadOnlyList<FieldParserModel> Fields { get; }
    }
}
