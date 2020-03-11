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
            OperationDefinitionNode operation,
            ComplexOutputTypeModel resultType,
            IReadOnlyList<ArgumentModel> arguments)
        {
            Name = name;
            Type = type;
            Operation = operation;
            ResultType = resultType;
            Arguments = arguments;
        }

        public string Name { get; }

        public ObjectType Type { get; }

        public OperationDefinitionNode Operation { get; }

        public ComplexOutputTypeModel ResultType { get; }

        public IReadOnlyList<ArgumentModel> Arguments { get; }
    }
}
