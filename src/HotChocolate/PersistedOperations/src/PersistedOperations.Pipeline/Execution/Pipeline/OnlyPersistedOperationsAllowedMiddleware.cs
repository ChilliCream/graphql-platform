using System.Collections.Immutable;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.PersistedOperations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OnlyPersistedOperationsAllowedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly PersistedOperationOptions _options;
    private readonly OperationResult _errorResult;

    private OnlyPersistedOperationsAllowedMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        PersistedOperationOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _diagnosticEvents = diagnosticEvents;

        // prepare options.
        _options = options;
        _errorResult = new OperationResult([options.OperationNotAllowedError])
        {
            ContextData = ImmutableDictionary<string, object?>.Empty.Add(ExecutionContextData.HttpStatusCode, 400)
        };
    }

    public ValueTask InvokeAsync(RequestContext context)
    {
        // if all operations are allowed.
        if (!_options.OnlyAllowPersistedDocuments || context.IsWarmupRequest())
        {
            return _next(context);
        }

        // if the document is a persisted operation document, then in general we can
        // skip this middleware.
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.IsPersisted)
        {
            // however, this could still be a standard GraphQL request that contains a document
            // that just matches a persisted operation document.
            // either this is allowed by the configuration and we can skip this middleware
            if (_options.AllowDocumentBody)
            {
                return _next(context);
            }

            // or we have to make sure that the GraphQL request is a persisted operation request.
            // if the operation request has no document, we can be sure that it's
            // a persisted operation request, and we can skip this middleware.
            if (context.Request.Document is null)
            {
                return _next(context);
            }
        }

        // Lastly, it might be that the request is allowed for the current session even
        // if it's not a persisted operation request.
        if (context.Features.Get<PersistedOperationRequestOverrides>()?.AllowNonPersistedOperation == true)
        {
            return _next(context);
        }

        // if we reach this point, we have to throw an error since the request is not allowed.
        _diagnosticEvents.UntrustedDocumentRejected(context);
        context.Result = _errorResult;
        return default;
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var options = core.SchemaServices.GetRequiredService<PersistedOperationOptions>();
                var middleware = new OnlyPersistedOperationsAllowedMiddleware(
                    next,
                    diagnosticEvents,
                    options);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OnlyPersistedOperationsAllowed);
}
