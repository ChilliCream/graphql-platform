using System.Collections.Generic;
using System.IO;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface ITypeModel
    {
        INamedType Type { get; }
    }

    public interface IParserModel
    {
        /// <summary>
        /// Gets the operation that this parser handles.
        /// </summary>
        OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the operation return type.
        /// </summary>
        ComplexInputTypeModel ReturnType { get; }

        /// <summary>
        /// Gets the leaf types that are used in this parser.
        /// </summary>
        IReadOnlyList<INamedType> LeafTypes { get; }

        /// <summary>
        /// Gets the field parser models.
        /// </summary>
        IReadOnlyList<IFieldParserModel> Fields { get; }
    }

    public interface IFieldParserModel
    {
        /// <summary>
        /// Gets the operation to which this field parser belongs to.
        /// </summary>
        OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the field selection that this parser handles.
        /// </summary>
        FieldNode Selection { get; }

        /// <summary>
        /// Gets the field path.
        /// </summary>
        Path Path { get; }

        /// <summary>
        /// Gets the return type of the field.
        /// </summary>
        ComplexInputTypeModel ReturnType { get; }

        /// <summary>
        /// Gets the possible types that this field can return.
        /// </summary>
        IReadOnlyList<ComplexOutputTypeModel> PossibleTypes { get; }
    }
}
