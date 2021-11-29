using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Errors;

internal class ReturnNullWhenErrorWasThrow
{
    private readonly FieldDelegate _next;

    public ReturnNullWhenErrorWasThrow(FieldDelegate next)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
    }

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var parent = context.Parent<object?>();

        if (parent == ErrorMiddleware.ErrorObject || parent is null)
        {
            context.Result = null;
            return default;
        }

        return _next(context);
    }
}
