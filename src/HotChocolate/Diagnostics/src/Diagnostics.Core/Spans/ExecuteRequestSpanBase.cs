using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal abstract class ExecuteRequestSpanBase(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public RequestContext Context { get; } = context;

    protected static Activity? StartActivity(ActivitySource source)
    {
        var activity = source.StartActivity("GraphQL Operation");

        activity?.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Request);

        return activity;
    }

    protected abstract bool TryGetOperationInfo(
        out OperationType operationType,
        out string? operationName);

    protected override void OnComplete()
    {
        // Ensures the root request span carries `graphql.processing.type=request`
        // even when an existing HTTP transport activity was reused as the root.
        if (Activity.GetTagItem(GraphQL.Processing.Type) is null)
        {
            Activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Request);
        }

        EnrichServerAttributes();

        string? operationTypeValue = null;
        string? operationName = null;
        if (TryGetOperationInfo(out var operationType, out operationName))
        {
            operationTypeValue = GraphQL.Operation.TypeValues[operationType];
            Activity.DisplayName = operationTypeValue;
            Activity.EnrichOperation(operationType, operationName);
        }

        var documentInfo = Context.OperationDocumentInfo;

        Activity.EnrichDocumentInfo(documentInfo);

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            Activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (Context.Result is OperationResult result)
        {
            if (result.Errors.Count > 0)
            {
                Activity.SetTag(GraphQL.Error.Count, result.Errors.Count);
            }

            EmitErrorEvents(result.Errors, operationTypeValue, operationName);
        }

        if (Context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);

            if (Context.Result is OperationResult { Errors: [var firstError, ..] })
            {
                Activity.SetErrorType(firstError, ActivityExtensions.ExecutionErrorType);
            }
            else
            {
                Activity.SetErrorType(ActivityExtensions.ExecutionErrorType);
            }
        }
        else if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichExecuteRequest(Context, Activity);
    }

    private void EnrichServerAttributes()
    {
        if (!Context.Features.TryGet<HttpContext>(out var httpContext))
        {
            return;
        }

        var request = httpContext.Request;
        if (!request.Host.HasValue)
        {
            return;
        }

        if (!string.IsNullOrEmpty(request.Host.Host)
            && Activity.GetTagItem(SemanticConventions.Server.Address) is null)
        {
            Activity.SetTag(SemanticConventions.Server.Address, request.Host.Host);
        }

        if (request.Host.Port is { } port
            && Activity.GetTagItem(SemanticConventions.Server.Port) is null)
        {
            var defaultPort = request.IsHttps ? 443 : 80;
            if (port != defaultPort)
            {
                Activity.SetTag(SemanticConventions.Server.Port, port);
            }
        }
    }

    private void EmitErrorEvents(
        IReadOnlyList<IError> errors,
        string? operationType,
        string? operationName)
    {
        var maxEvents = options.MaxErrorEvents;
        if (maxEvents <= 0 || errors.Count == 0)
        {
            return;
        }

        var limit = errors.Count < maxEvents ? errors.Count : maxEvents;
        for (var i = 0; i < limit; i++)
        {
            Activity.AddGraphQLErrorEvent(errors[i], operationType, operationName);
        }
    }
}
