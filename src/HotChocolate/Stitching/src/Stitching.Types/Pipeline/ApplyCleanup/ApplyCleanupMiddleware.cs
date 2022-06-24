using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;

/// <summary>
/// Removes Types and Fields which have the remove directive associated
/// </summary>
public sealed class ApplyCleanupMiddleware
{
    private readonly CleanupIndexer _indexer = new();
    private readonly CleanupRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyCleanupMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document doc = context.Documents[i];

            var cleanupContext = new CleanupContext(doc.Name);
            _indexer.Visit(doc.SyntaxTree, cleanupContext);

            var syntaxTree = (DocumentNode?) _rewriter.Rewrite(doc.SyntaxTree, cleanupContext);
            if (ReferenceEquals(doc.SyntaxTree, syntaxTree))
            {
                continue;
            }

            if (syntaxTree is not null)
            {
                context.Documents = context.Documents.SetItem(i, new Document(doc.Name, syntaxTree));
            }
            else
            {
                context.Documents = context.Documents.RemoveAt(i);
            }
        }

        await _next(context);
    }
}
