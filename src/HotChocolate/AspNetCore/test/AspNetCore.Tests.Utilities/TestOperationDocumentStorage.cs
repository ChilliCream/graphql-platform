using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public sealed class TestOperationDocumentStorage : IOperationDocumentStorage
{
    private readonly Dictionary<string, DocumentNode> _cache = new();

    public TestOperationDocumentStorage()
    {
        _cache.Add(
            "60ddx_GGk4FDObSa6eK0sg",
            Utf8GraphQLParser.Parse(@"query GetHeroName { hero { name } }"));

        _cache.Add(
            "abc123",
            Utf8GraphQLParser.Parse(@"query Test($if: Boolean) { hero { name @skip(if: $if) } }"));

        _cache.Add(
            "a73defcdf38e5891e91b9ba532cf4c36",
            Utf8GraphQLParser.Parse(@"query GetHeroName { hero { name } }"));
    }

    public async ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(documentId.Value, out var document))
        {
            return await Task.FromResult(new OperationDocument(document));
        }

        return null;
    }

    public ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        _cache[documentId.Value] = Utf8GraphQLParser.Parse(document.AsSpan());
        return default;
    }
}
