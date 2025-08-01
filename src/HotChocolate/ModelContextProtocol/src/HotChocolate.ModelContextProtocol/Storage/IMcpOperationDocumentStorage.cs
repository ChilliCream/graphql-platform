using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

public interface IMcpOperationDocumentStorage
{
    ValueTask<Dictionary<string, DocumentNode>> GetToolDocumentsAsync(
        CancellationToken cancellationToken = default);

    ValueTask SaveToolDocumentAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default);
}
