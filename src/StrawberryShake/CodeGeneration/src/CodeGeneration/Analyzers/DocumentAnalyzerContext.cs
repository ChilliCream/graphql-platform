using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class DocumentAnalyzerContext : IDocumentAnalyzerContext
{
    private readonly HashSet<string> _takenNames = new(StringComparer.Ordinal);
    private readonly Dictionary<ISyntaxNode, HashSet<string>> _syntaxNodeNames = new();
    private readonly Dictionary<string, ITypeModel> _typeModels = new(StringComparer.Ordinal);
    private readonly Dictionary<SelectionSetInfo, SelectionSetNode> _selectionSets = new();
    private readonly FieldCollector _fieldCollector;

    public DocumentAnalyzerContext(
        ISchema schema,
        DocumentNode document)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Document = document ?? throw new ArgumentNullException(nameof(document));
        OperationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        OperationType = schema.GetOperationType(OperationDefinition.Operation)!;
        OperationName = OperationDefinition.Name!.Value;
        RootPath = Path.Root.Append(OperationName);

        _fieldCollector = new FieldCollector(schema, document);
    }

    public ISchema Schema { get; }

    public DocumentNode Document { get; }

    public ObjectType OperationType { get; }

    public OperationDefinitionNode OperationDefinition { get; }

    public string OperationName { get; }

    public Path RootPath { get; }

    public Queue<FieldSelection> Fields { get; } = new();

    public IReadOnlyCollection<ITypeModel> TypeModels => _typeModels.Values;

    public IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> SelectionSets =>
        _selectionSets;

    public SelectionSetVariants CollectFields() =>
        _fieldCollector.CollectFields(
            OperationDefinition.SelectionSet,
            OperationType,
            Path.Root.Append(OperationName));

    public SelectionSetVariants CollectFields(FieldSelection fieldSelection) =>
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
        string name,
        [NotNullWhen(true)] out T? typeModel)
        where T : ITypeModel
    {
        if (_typeModels.TryGetValue(name, out var model) &&
            model is T casted)
        {
            typeModel = casted;
            return true;
        }

        typeModel = default;
        return false;
    }

    public void RegisterModel(string name, ITypeModel typeModel)
    {
        if (_typeModels.TryGetValue(name, out var registeredTypeModel) &&
            !ReferenceEquals(registeredTypeModel, typeModel))
        {
            throw new GraphQLException("A type model name must be unique.");
        }

        _typeModels[name] = typeModel;
    }

    public void RegisterType(INamedType type)
    {
        if (type is ILeafType leafType && _typeModels.Values.All(x => x.Type.Name != type.Name))
        {
            _typeModels.Add(
                leafType.Name,
                new LeafTypeModel(
                    leafType.Name,
                    leafType.Description,
                    leafType,
                    leafType.GetSerializationType(),
                    leafType.GetRuntimeType()));
        }
    }

    public void RegisterSelectionSet(
        INamedType namedType,
        SelectionSetNode from,
        SelectionSetNode to)
    {
        var key = new SelectionSetInfo(namedType, from);

        if (_selectionSets.TryGetValue(key, out var selectionSet) && !
                ReferenceEquals(selectionSet, to))
        {
            throw new GraphQLException("A selection-set lookup must be unique.");
        }

        _selectionSets[key] = to;
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

    public string ResolveTypeName(string proposedName)
    {
        if (_takenNames.Add(proposedName))
        {
            return proposedName;
        }

        for (var i = 1; i < 1000000; i++)
        {
            var alternativeName = proposedName + "_" + i;

            if (_takenNames.Add(alternativeName))
            {
                return alternativeName;
            }
        }

        throw new InvalidOperationException(
            "Unable to find a name for the specified syntax node.");
    }

    public string ResolveTypeName(
        string proposedName,
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
            takenNames = new HashSet<string>(StringComparer.Ordinal);
            _syntaxNodeNames.Add(syntaxNode, takenNames);
        }

        if (_takenNames.Add(proposedName))
        {
            takenNames.Add(proposedName);
            return proposedName;
        }

        for (var i = 1; i < 1000000; i++)
        {
            var alternativeName = proposedName + "_" + i;

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
