using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types;

public class ApplyExtensionsMiddleware
{
    private readonly MergeSchema _next;

    public ApplyExtensionsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        



        await _next(context);
    }
}
