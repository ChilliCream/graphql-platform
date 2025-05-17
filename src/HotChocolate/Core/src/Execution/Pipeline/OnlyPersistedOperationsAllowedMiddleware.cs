using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.Options.PersistedOperationOptions;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OnlyPersistedOperationsAllowedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly PersistedOperationOptions _options;
    private readonly IOperationResult _errorResult;
    private readonly GraphQLException _exception;
    private readonly Dictionary<string, object?> _statusCode = new() { { WellKnownContextData.HttpStatusCode, 400 }, };

    private OnlyPersistedOperationsAllowedMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IPersistedOperationOptionsAccessor options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));

        // prepare options.
        _options = options.PersistedOperations;
        var error = options.PersistedOperations.OperationNotAllowedError;
        _errorResult =  OperationResultBuilder.CreateError(error, _statusCode);
        _exception = new GraphQLException(error);
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        // if all operations are allowed or the request is a warmup request, we can skip this middleware.
        if(!_options.OnlyAllowPersistedDocuments || context.IsWarmupRequest())
        {
            return _next(context);
        }

        // if the document is a persisted operation document than in general we can
        // skip this middleware.
        if(context.IsPersistedDocument)
        {
            // however this could still be a standard GraphQL request that contains a document
            // that just matches a persisted operation document.
            // either this is allowed by the configuration and we can skip this middleware
            if (_options.AllowDocumentBody)
            {
                return _next(context);
            }

            // or we have to make sure that the GraphQL request is a persisted operation request.
            // if the operation request has no document we can be sure that it's
            // a persisted operation request and we can skip this middleware.
            if (context.Request.Document is null)
            {
                return _next(context);
            }
        }

        // lastly it might be that the request is allowed for the current session even
        // if it's not a persisted operation request.
        if (context.ContextData.ContainsKey(WellKnownContextData.NonPersistedOperationAllowed))
        {
            return _next(context);
        }

        // if we reach this point we have to throw an error since the request is not allowed.
        _diagnosticEvents.RequestError(context, _exception);
        context.Result = _errorResult;
        return default;
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var options = core.SchemaServices.GetRequiredService<IRequestExecutorOptionsAccessor>();
            var middleware = new OnlyPersistedOperationsAllowedMiddleware(next, diagnosticEvents, options);
            return context => middleware.InvokeAsync(context);
        };
}
