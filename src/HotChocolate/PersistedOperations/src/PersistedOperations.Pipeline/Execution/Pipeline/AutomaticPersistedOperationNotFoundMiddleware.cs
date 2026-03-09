using System.Collections.Immutable;

namespace HotChocolate.Execution.Pipeline;

internal sealed class AutomaticPersistedOperationNotFoundMiddleware
{
    private static readonly OperationResult s_errorResult =
        new([PersistedOperationNotFound()])
        {
            ContextData = ImmutableDictionary<string, object?>.Empty.Add(ExecutionContextData.HttpStatusCode, 400)
        };

    private readonly RequestDelegate _next;

    private AutomaticPersistedOperationNotFoundMiddleware(
        RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
    }

    public ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is not null || context.Request.Document is not null)
        {
            return _next(context);
        }

        var result = s_errorResult;
        context.Result = result;
        return default;
    }

    public static IError PersistedOperationNotFound()
        => ErrorBuilder.New()
            // this string is defined in the APQ spec!
            .SetMessage("PersistedQueryNotFound")
            .SetCode(ErrorCodes.Execution.PersistedOperationNotFound)
            .Build();

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            static (_, next) =>
            {
                var middleware = new AutomaticPersistedOperationNotFoundMiddleware(next);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.AutomaticPersistedOperationNotFoundMiddleware);
}
