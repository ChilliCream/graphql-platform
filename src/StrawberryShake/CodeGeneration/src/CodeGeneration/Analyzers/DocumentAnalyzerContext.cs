using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class DocumentAnalyzerContext : IDocumentAnalyzerContext
    {
        private readonly HashSet<NameString> _takenNames = new();
        private readonly Dictionary<ISyntaxNode, HashSet<NameString>> _syntaxNodeNames = new();
        private readonly Dictionary<NameString, ITypeModel> _typeModels = new();
        private readonly Dictionary<SelectionSetInfo, SelectionSetNode> _selectionSets = new();
        private readonly FieldCollector _fieldCollector;

        public DocumentAnalyzerContext(
            ISchema schema,
            DocumentNode document)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(document));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            OperationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
            OperationType = schema.GetOperationType(OperationDefinition.Operation);
            OperationName = OperationDefinition.Name!.Value;
            RootPath = Path.Root.Append(OperationName);

            _fieldCollector = new FieldCollector(schema, document);
        }

        public ISchema Schema { get; }

        public DocumentNode Document { get; }

        public ObjectType OperationType { get; }

        public OperationDefinitionNode OperationDefinition { get; }

        public NameString OperationName { get; }

        public Path RootPath { get; }

        public Queue<FieldSelection> Fields { get; } = new();

        public IReadOnlyCollection<ITypeModel> TypeModels => _typeModels.Values;

        public IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> SelectionSets =>
            _selectionSets;

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

        public bool TryGetModel<T>(
            NameString name,
            [NotNullWhen(true)] out T? typeModel)
            where T : ITypeModel
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

        public void RegisterType(INamedType type)
        {
            if (type is ILeafType leafType)
            {
                if (!_typeModels.ContainsKey(type.Name))
                {
                    string runtimeType = leafType.GetRuntimeType();
                    string serializationType = leafType.GetSerializationType();

                    _typeModels.Add(
                        leafType.Name,
                        new LeafTypeModel(
                            leafType.Name,
                            leafType.Description,
                            leafType,
                            serializationType,
                            runtimeType));
                }
            }
        }

        public void RegisterSelectionSet(
            INamedType namedType,
            SelectionSetNode from,
            SelectionSetNode to) =>
            _selectionSets.Add(new(namedType, from), to);

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
            if (_takenNames.Add(proposedName))
            {
                return proposedName;
            }

            for (var i = 1; i < 1000000; i++)
            {
                NameString alternativeName = proposedName + "_" + i;

                if (_takenNames.Add(alternativeName))
                {
                    return alternativeName;
                }
            }

            throw new InvalidOperationException(
                "Unable to find a name for the specified syntax node.");
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
                _syntaxNodeNames.Add(syntaxNode, takenNames);
            }

            if (_takenNames.Add(proposedName))
            {
                takenNames.Add(proposedName);
                return proposedName;
            }

            for (var i = 1; i < 1000000; i++)
            {
                NameString alternativeName = proposedName + "_" + i;

                if (takenNames.Contains(alternativeName))
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
