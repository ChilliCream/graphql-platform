using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Analyzers;

public partial class DocumentAnalyzer
{
    private readonly List<DocumentNode> _documents = [];
    private Schema? _schema;

    public static DocumentAnalyzer New() => new();

    public DocumentAnalyzer SetSchema(Schema schema)
    {
        _schema = schema;
        return this;
    }

    public DocumentAnalyzer AddDocument(DocumentNode document)
    {
        _documents.Add(document);
        return this;
    }

    public ClientModel Analyze()
    {
        if (_schema is null)
        {
            throw new InvalidOperationException(
                "You must provide a schema.");
        }

        if (_documents.Count == 0)
        {
            throw new InvalidOperationException(
                "You must at least provide one document.");
        }

        var operationDocuments = CreateOperationDocuments(_documents, _schema);
        List<OperationModel> operations = [];
        Dictionary<string, LeafTypeModel> leafTypes = new(StringComparer.Ordinal);
        Dictionary<string, InputObjectTypeModel> inputObjectType = new(StringComparer.Ordinal);
        Dictionary<SelectionSetInfo, SelectionSetNode> selectionSets = [];

        foreach (var operation in operationDocuments.Operations.Values)
        {
            var context = new DocumentAnalyzerContext(_schema, operation);
            var operationModel = CreateOperationModel(context);
            operations.Add(operationModel);

            foreach (var typeModel in context.TypeModels)
            {
                if (typeModel is LeafTypeModel leafTypeModel)
                {
                    leafTypes.TryAdd(leafTypeModel.Name, leafTypeModel);
                }
                else if (typeModel is InputObjectTypeModel inputObjectTypeModel)
                {
                    inputObjectType.TryAdd(inputObjectTypeModel.Name, inputObjectTypeModel);
                }
            }

            foreach (var (key, value) in context.SelectionSets)
            {
                if (selectionSets.TryGetValue(key, out var to) && to != value)
                {
                    throw ThrowHelper.DuplicateSelectionSet();
                }

                selectionSets[key] = value;
            }
        }

        return new ClientModel(
            _schema,
            operations,
            leafTypes.Values.ToList(),
            inputObjectType.Values.ToList());
    }
}
