using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class PersistedOperationNotFoundMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly Dictionary<string, object?> _statusCode = new() { { HttpStatusCode, 400 }, };

    private PersistedOperationNotFoundMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is not null || context.Request.Document is not null)
        {
            return _next(context);
        }

        // we know that the key is not null since otherwise the request would have
        // failed already since no operation is specified.
        var requestedKey =
            (context.Request.DocumentId ??
            context.DocumentId ??
            context.DocumentHash ??
            context.Request.DocumentHash)!.Value;

        _diagnosticEvents.DocumentNotFoundInStorage(context, requestedKey);
        var error = ErrorHelper.PersistedOperationNotFound(requestedKey);
        _diagnosticEvents.RequestError(context, new GraphQLException(error));
        context.Result = OperationResultBuilder.CreateError(error, _statusCode);

        return default;
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var middleware = new PersistedOperationNotFoundMiddleware(next, diagnosticEvents);
            return context => middleware.InvokeAsync(context);
        };
}
