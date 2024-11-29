namespace HotChocolate.Types;

internal sealed class ErrorNullMiddleware(FieldDelegate next)
{
    private readonly FieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        if (context.ScopedContextData.ContainsKey(ErrorContextDataKeys.Errors))
        {
            context.Result = null;
        }
        else
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
