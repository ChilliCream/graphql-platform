using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class AutomaticPersistedOperationNotFoundMiddleware
{
    private static readonly IOperationResult s_errorResult =
        OperationResultBuilder.CreateError(
            PersistedOperationNotFound(),
            contextData: new Dictionary<string, object?>
            {
                { ExecutionContextData.HttpStatusCode, 400 }
            });
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    private AutomaticPersistedOperationNotFoundMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
    }

    public ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is not null || context.Request.Document is not null)
        {
            return _next(context);
        }

        var error = PersistedOperationNotFound();
        var result = s_errorResult;

        _diagnosticEvents.ExecutionError(context, ErrorKind.RequestError, [error]);
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
            static (factoryContext, next) =>
            {
                var diagnosticEvents = factoryContext.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new AutomaticPersistedOperationNotFoundMiddleware(next, diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            nameof(AutomaticPersistedOperationNotFoundMiddleware));
}
