using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyMissingBindings;

public class ApplyMissingBindingsMiddleware
{
    private readonly BindingRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyMissingBindingsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document document = context.Documents[i];
            DocumentNode syntaxTree = document.SyntaxTree;

            var bindContext = new BindingContext(document.Name);
            syntaxTree = (DocumentNode)_rewriter.Rewrite(syntaxTree, bindContext);
            document = new Document(document.Name, syntaxTree);

            context.Documents = context.Documents.SetItem(i, document);
        }

        await _next(context);
    }
}
