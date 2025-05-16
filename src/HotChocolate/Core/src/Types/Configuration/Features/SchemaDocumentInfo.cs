#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Configuration;

internal sealed class SchemaDocumentInfo
{
    private readonly Func<IServiceProvider, DocumentNode>? _loadDocument;

    public SchemaDocumentInfo(Func<IServiceProvider, DocumentNode> loadDocument)
    {
        ArgumentNullException.ThrowIfNull(loadDocument);
        _loadDocument = loadDocument;
    }

    public SchemaDocumentInfo(DocumentNode documentNode)
    {
        ArgumentNullException.ThrowIfNull(documentNode);
        DocumentNode = documentNode;
    }

    public DocumentNode? DocumentNode { get; private set; }

    public DocumentNode Load(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        DocumentNode ??= _loadDocument?.Invoke(services);
        return DocumentNode!;
    }
}
