using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class OperationModel
    {
        public OperationModel(
            NameString name,
            ObjectType type,
            DocumentNode document,
            OperationDefinitionNode operation,
            IReadOnlyList<ArgumentModel> arguments,
            OutputTypeModel resultType,
            IReadOnlyList<LeafTypeModel> leafTypes,
            IReadOnlyList<InputObjectTypeModel> inputObjectTypes,
            IReadOnlyList<OutputTypeModel> outputTypeModels)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Type = type ??
                throw new ArgumentNullException(nameof(type));
            Document = document ??
                throw new ArgumentNullException(nameof(document));
            Operation = operation ??
                throw new ArgumentNullException(nameof(operation));
            Arguments = arguments ??
                throw new ArgumentNullException(nameof(arguments));
            ResultType = resultType ??
                throw new ArgumentNullException(nameof(resultType));
            LeafTypes = leafTypes ??
                throw new ArgumentNullException(nameof(leafTypes));
            InputObjectTypes = inputObjectTypes ??
                throw new ArgumentNullException(nameof(inputObjectTypes));
            OutputTypes = outputTypeModels ??
                throw new ArgumentNullException(nameof(outputTypeModels));
        }

        public string Name { get; }

        public ObjectType Type { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Operation { get; }

        public IReadOnlyList<ArgumentModel> Arguments { get; }

        public OutputTypeModel ResultType { get; }

        public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

        public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }

        public IReadOnlyList<OutputTypeModel> OutputTypes { get; }

        public IEnumerable<OutputTypeModel> GetImplementations(OutputTypeModel outputType)
        {
            if (outputType is null)
            {
                throw new ArgumentNullException(nameof(outputType));
            }

            foreach (var model in OutputTypes)
            {
                if (model.Implements.Contains(outputType))
                {
                    yield return model;
                }
            }
        }

        public OutputTypeModel GetFieldResultType(FieldNode fieldSyntax)
        {
            if (fieldSyntax is null)
            {
                throw new ArgumentNullException(nameof(fieldSyntax));
            }

            return OutputTypes.First(
                t => t.IsInterface && t.SelectionSet == fieldSyntax.SelectionSet);
        }

        public bool TryGetFieldResultType(
            FieldNode fieldSyntax, 
            [NotNullWhen(true)]out OutputTypeModel? fieldType)
        {
            if (fieldSyntax is null)
            {
                throw new ArgumentNullException(nameof(fieldSyntax));
            }

            fieldType = OutputTypes.FirstOrDefault(
                t => t.IsInterface && t.SelectionSet == fieldSyntax.SelectionSet);
            return fieldType is not null;
        }
    }
}
