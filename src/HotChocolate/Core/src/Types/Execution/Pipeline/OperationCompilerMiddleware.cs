using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationCompilerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OperationCompiler _operationCompiler;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private OperationCompilerMiddleware(
        RequestDelegate next,
        ISchemaDefinition schema,
        OperationCompiler operationCompiler,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operationCompiler);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _operationCompiler = operationCompiler;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        if (context.TryGetOperation(out var operation, out var operationId))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.Document is not null && documentInfo.Id.HasValue && documentInfo.IsValidated)
        {
            var normalizedDocument = context.GetNormalizedDocument();
            var inFlightOperation = context.Features.Get<TaskCompletionSource<Operation>>();

            using (_diagnosticEvents.CompileOperation(context))
            {
                try
                {
                    operation = _operationCompiler.Compile(
                        operationId ?? Guid.NewGuid().ToString("N"),
                        documentInfo.Hash.Value,
                        context.Request.OperationName,
                        normalizedDocument);

                    context.SetOperation(operation);
                    inFlightOperation?.TrySetResult(operation);
                }
                catch (Exception ex)
                {
                    inFlightOperation?.TrySetException(ex);
                    throw;
                }
            }

            await _next(context).ConfigureAwait(false);
            return;
        }

        context.Result = StateInvalidForOperationResolver();
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var schema = core.Schema;
                var operationCompiler = core.SchemaServices.GetRequiredService<OperationCompiler>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

                var middleware = new OperationCompilerMiddleware(
                    next,
                    schema,
                    operationCompiler,
                    diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OperationCompilerMiddleware);
}
