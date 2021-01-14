using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models2;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal interface IDocumentAnalyzerContext
    {
        ISchema Schema { get; }

        ObjectType OperationType { get; }

        OperationDefinitionNode OperationDefinition { get; }

        NameString OperationName { get; }

        Queue<FieldSelection> Fields { get; }

        NameString ResolveTypeName(
            NameString proposedName);

        NameString ResolveTypeName(
            NameString proposedName,
            ISyntaxNode syntaxNode,
            IReadOnlyList<string>? additionalNamePatterns = null);

        SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path);

        bool TryGetModel<T>(
            NameString name,
            [NotNullWhen(true)] out T? typeModel)
            where T : ITypeModel;

        void RegisterModel(NameString name, ITypeModel typeModel);

        IEnumerable<OutputTypeModel> GetImplementations(OutputTypeModel outputTypeModel);
    }

    public class DocumentAnalyzerContext : IDocumentAnalyzerContext
    {
        private readonly HashSet<NameString> _takenNames = new();
        private readonly Dictionary<ISyntaxNode, HashSet<NameString>> _syntaxNodeNames = new();
        private readonly Dictionary<NameString, ITypeModel> _typeModels = new();
        private readonly FieldCollector _fieldCollector;

        public DocumentAnalyzerContext(
            ISchema schema,
            DocumentNode document)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Schema = schema;
            OperationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
            OperationType = schema.GetOperationType(OperationDefinition.Operation);
            OperationName = OperationDefinition.Name!.Value;

            _fieldCollector = new FieldCollector(schema, document);
        }

        public ISchema Schema { get; }

        public ObjectType OperationType { get; }

        public OperationDefinitionNode OperationDefinition { get; }

        public NameString OperationName { get; }

        public Queue<FieldSelection> Fields { get; } = new();

        public SelectionSetVariants CollectFields() =>
            _fieldCollector.CollectFields(
                OperationDefinition.SelectionSet,
                OperationType,
                Path.New(OperationName));

        public SelectionSetVariants CollectFields(
            FieldSelection fieldSelection) =>
            CollectFields(
                fieldSelection.SyntaxNode.SelectionSet!,
                (INamedOutputType)fieldSelection.Field.Type.NamedType(),
                fieldSelection.Path);

        public SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path) =>
            _fieldCollector.CollectFields(
                selectionSet,
                type,
                path);

        public bool TryGetModel<T>(NameString name, out T? typeModel) where T : ITypeModel
        {
            if (_typeModels.TryGetValue(name, out ITypeModel? model) &&
                model is T casted)
            {
                typeModel = casted;
                return true;
            }

            typeModel = default;
            return false;
        }

        public void RegisterModel(NameString name, ITypeModel typeModel)
        {
            _typeModels.Add(name, typeModel);
        }

        public IEnumerable<OutputTypeModel> GetImplementations(OutputTypeModel outputTypeModel)
        {
            foreach (var model in _typeModels.Values.OfType<OutputTypeModel>())
            {
                if (model.Implements.Contains(outputTypeModel))
                {
                    yield return model;
                }
            }
        }

        public NameString ResolveTypeName(
            NameString proposedName)
        {
            throw new System.NotImplementedException();
        }

        public NameString ResolveTypeName(
            NameString proposedName,
            ISyntaxNode syntaxNode,
            IReadOnlyList<string>? additionalNamePatterns = null)
        {
            if (_syntaxNodeNames.TryGetValue(syntaxNode, out var takenNames) &&
                takenNames.Contains(proposedName))
            {
                return proposedName;
            }

            if (!_syntaxNodeNames.TryGetValue(syntaxNode, out takenNames))
            {
                takenNames = new HashSet<NameString>();
            }

            if (_takenNames.Add(proposedName))
            {
                takenNames.Add(proposedName);
                return proposedName;
            }

            for (var i = 1; i < 1000; i++)
            {
                NameString alternativeName = proposedName + "_" + i;

                if (_takenNames.Contains(alternativeName))
                {
                    return alternativeName;
                }

                if (_takenNames.Add(alternativeName))
                {
                    takenNames.Add(alternativeName);
                    return alternativeName;
                }
            }

            throw new InvalidOperationException(
                "Unable to find a name for the specified syntax node.");
        }
    }
}
