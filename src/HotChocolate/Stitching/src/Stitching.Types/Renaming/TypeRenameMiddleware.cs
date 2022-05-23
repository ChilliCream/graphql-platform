using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types.Renaming;

public class TypeRenameMiddleware
{
    private readonly RewriteRequestDelegate _next;

    public TypeRenameMiddleware(RewriteRequestDelegate next)
    {
        _next = next;
    }

    public virtual ValueTask NextAsync(IRewriteContext context)
    {
        var collectDirectiveVisitor = new CollectTypeRenameDirective<IRewriteContext>();
        collectDirectiveVisitor.Visit(context);

        var visitor = new RenameTypes<IRewriteContext>(collectDirectiveVisitor.Directives);
        context.Document = visitor.Rewrite(context);

        return _next(context);
    }
}
