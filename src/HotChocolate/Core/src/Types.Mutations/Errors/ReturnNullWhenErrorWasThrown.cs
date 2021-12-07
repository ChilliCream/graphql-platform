namespace HotChocolate.Types;

internal sealed class ReturnNullWhenErrorWasThrown
{
    private readonly FieldDelegate _next;

    public ReturnNullWhenErrorWasThrown(FieldDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
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
