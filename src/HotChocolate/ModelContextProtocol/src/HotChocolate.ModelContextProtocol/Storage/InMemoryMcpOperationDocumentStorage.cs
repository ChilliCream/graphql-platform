using CaseConverter;
using HotChocolate.Language;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Storage;

public sealed class InMemoryMcpOperationDocumentStorage : IMcpOperationDocumentStorage
{
    private readonly Dictionary<string, DocumentNode> _tools = [];

    public ValueTask<Dictionary<string, DocumentNode>> GetToolDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_tools);
    }

    public ValueTask SaveToolDocumentAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        var operationDefinitions = document.Definitions
            .OfType<OperationDefinitionNode>()
            .ToList();

        if (operationDefinitions.Count != 1)
        {
            throw new Exception(
                InMemoryMcpOperationDocumentStorage_ToolDocumentMustContainSingleOperation);
        }

        if (operationDefinitions[0].Name is not { } nameNode)
        {
            throw new Exception(
                InMemoryMcpOperationDocumentStorage_ToolDocumentOperationMustBeNamed);
        }

        if (!_tools.TryAdd(nameNode.Value.ToSnakeCase(), document))
        {
            throw new Exception(
                InMemoryMcpOperationDocumentStorage_ToolDocumentOperationAlreadyExists);
        }

        return ValueTask.CompletedTask;
    }
}
