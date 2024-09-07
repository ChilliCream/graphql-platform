using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OnlyPersistedOperationsAllowedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly bool _allowAllOperations;
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
        _allowAllOperations = !options.OnlyAllowPersistedOperations;
        var error = options.OnlyPersistedOperationsAreAllowedError;
        _errorResult =  OperationResultBuilder.CreateError(error, _statusCode);
        _exception = new GraphQLException(error);
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (_allowAllOperations ||
            context.Request.Document is null ||
            context.ContextData.ContainsKey(WellKnownContextData.NonPersistedOperationAllowed))
        {
            return _next(context);
        }

        // we know that the key is not null since otherwise the request would have
        // failed already since no operation is specified.
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
