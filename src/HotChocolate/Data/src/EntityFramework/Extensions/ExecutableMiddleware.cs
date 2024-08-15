using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types;

#pragma warning disable CA1812

internal sealed class ExecutableMiddleware(FieldDelegate next)
{
    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        await next(context).ConfigureAwait(false);

        if (context.Result is IExecutable executable)
        {
            context.Result = await executable
                .ToListAsync(context.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
