using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public sealed class FileSchemaDocumentResolver : ISchemaDocumentResolver
{
    public FileSchemaDocumentResolver(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(
                $"'{nameof(sourceText)}' cannot be null or empty.", 
                nameof(sourceText));
        }

        Document = Utf8GraphQLParser.Parse(sourceText);
    }

    public FileSchemaDocumentResolver(DocumentNode document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public DocumentNode Document { get; }

    public Task<DocumentNode> GetDocumentAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(Document);
}
