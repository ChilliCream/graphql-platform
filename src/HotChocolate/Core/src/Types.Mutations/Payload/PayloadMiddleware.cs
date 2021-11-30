#nullable enable

namespace HotChocolate.Types;

internal class PayloadMiddleware
{
    public static readonly string MiddlewareIdentifier =
        "HotChocolate.Types.Payload.PayloadMiddleware";

    private readonly FieldDelegate _next;

    public PayloadMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);
        context.Result = new Payload(context.Result);
    }
}
