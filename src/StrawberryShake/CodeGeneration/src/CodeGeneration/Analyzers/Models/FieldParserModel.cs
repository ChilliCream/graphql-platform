using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class FieldParserModel
    {
        public FieldParserModel(
            OperationDefinitionNode operation,
            FieldNode selection,
            Path path,
            ComplexInputTypeModel returnType,
            IReadOnlyList<ComplexOutputTypeModel> possibleTypes)
        {
            Operation = operation;
            Selection = selection;
            Path = path;
            ReturnType = returnType;
            PossibleTypes = possibleTypes;
        }

        /// <summary>
        /// Gets the operation to which this field parser belongs to.
        /// </summary>
        public OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the field selection that this parser handles.
        /// </summary>
        public FieldNode Selection { get; }

        /// <summary>
        /// Gets the field path.
        /// </summary>
        public Path Path { get; }

        /// <summary>
        /// Gets the return type of the field.
        /// </summary>
        public ComplexInputTypeModel ReturnType { get; }

        /// <summary>
        /// Gets the possible types that this field can return.
        /// </summary>
        public IReadOnlyList<ComplexOutputTypeModel> PossibleTypes { get; }
    }
}
