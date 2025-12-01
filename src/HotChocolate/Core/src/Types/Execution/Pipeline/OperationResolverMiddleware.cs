using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Rewriters;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OperationCompiler _operationPlanner;
    private readonly DocumentRewriter _documentRewriter;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private OperationResolverMiddleware(
        RequestDelegate next,
        ISchemaDefinition schema,
        OperationCompiler operationPlanner,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operationPlanner);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _operationPlanner = operationPlanner;
        _documentRewriter = new DocumentRewriter(schema, removeStaticallyExcludedSelections: true);
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
        if (documentInfo.Document is not null && documentInfo.IsValidated)
        {
            using (_diagnosticEvents.CompileOperation(context))
            {
                // Before we can plan an operation, we must de-fragmentize it and remove static include conditions.
                var operationDocument = documentInfo.Document;
                var operationName = context.Request.OperationName;
                operationDocument = _documentRewriter.RewriteDocument(operationDocument, operationName);
                var operationNode = operationDocument.GetOperation(operationName);

                // After optimizing the query structure we can begin the planning process.
                operation = _operationPlanner.Compile(
                    operationId ?? Guid.NewGuid().ToString("N"),
                    documentInfo.Hash.Value,
                    operationNode);

                context.SetOperation(operation);
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
                var operationCompiler = core.Services.GetRequiredService<OperationCompiler>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

                var middleware = new OperationResolverMiddleware(
                    next,
                    schema,
                    operationCompiler,
                    diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OperationResolverMiddleware);
}
