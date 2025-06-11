using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class PersistedOperationNotFoundMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly Dictionary<string, object?> _statusCode = new() { { ExecutionContextData.HttpStatusCode, 400 } };

    private PersistedOperationNotFoundMiddleware(
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
        // checks if an operation document is present in the request.
        // we will accept either user supplied documents or documents
        // that are already present in the context.
        var documentExists =
            context.OperationDocumentInfo.Document is not null
                || context.Request.Document is not null;

        // if a document exists we simply continue with the next middleware.
        if (documentExists)
        {
            return _next(context);
        }

        // since no document exists we will throw a persisted operation not found error.
        // we can throw it as if there is no document present we know that a document id
        // must be present, otherwise the request would not have been routed to this middleware.
        _diagnosticEvents.DocumentNotFoundInStorage(context, context.Request.DocumentId);
        var error = PersistedOperationNotFound(context.Request.DocumentId);
        _diagnosticEvents.ExecutionError(context, ErrorKind.RequestError,  [error]);
        context.Result = OperationResultBuilder.CreateError(error, _statusCode);

        return default;
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new PersistedOperationNotFoundMiddleware(next, diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            nameof(PersistedOperationNotFoundMiddleware));

    public static IError PersistedOperationNotFound(OperationDocumentId requestedKey)
        => ErrorBuilder.New()
            .SetMessage("The specified persisted operation key is invalid.")
            .SetCode(ErrorCodes.Execution.PersistedOperationNotFound)
            .SetExtension(nameof(requestedKey), requestedKey)
            .Build();
}
