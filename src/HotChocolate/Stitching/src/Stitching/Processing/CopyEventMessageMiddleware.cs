using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Processing;

internal sealed class CopyEventMessageMiddleware
{
    private readonly FieldDelegate _next;

    public CopyEventMessageMiddleware(FieldDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        context.Result = context.GetEventMessage<IQueryResult>();
        return _next(context);
    }
}
