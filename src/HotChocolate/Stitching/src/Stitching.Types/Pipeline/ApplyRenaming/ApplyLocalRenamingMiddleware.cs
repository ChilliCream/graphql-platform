using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

public sealed class ApplyLocalRenamingMiddleware
{
    private readonly RenameIndexer _indexer = new();
    private readonly RenameRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyLocalRenamingMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document doc = context.Documents[i];

            var rewriteContext = new RewriteContext(doc.Name);
            _indexer.Visit(doc.SyntaxTree, rewriteContext);
            var syntaxTree = (DocumentNode)_rewriter.Rewrite(doc.SyntaxTree, rewriteContext);
            context.Documents = context.Documents.SetItem(i, new Document(doc.Name, syntaxTree));
        }

        await _next(context);
    }
}
