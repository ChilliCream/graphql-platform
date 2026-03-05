using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Fusion.Diagnostics.HotChocolateFusionActivitySource;

namespace HotChocolate.Fusion.Diagnostics.Listeners;

internal sealed class FusionActivityExecutionDiagnosticEventListener(
    FusionActivityEnricher enricher,
    InstrumentationOptions options) : FusionExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        Activity? httpContextActivity = null;

        if (options.SkipExecuteRequest)
        {
            if (!options.SkipExecuteHttpRequest
                && context.Features.TryGet<HttpContext>(out var httpContext)
                && httpContext.Features.Get<ExecuteHttpRequestSpan>() is { } httpRequestSpan)
            {
                httpContextActivity = httpRequestSpan.Activity;
            }
            else
            {
                return EmptyScope;
            }
        }

        var span = httpContextActivity is not null
            ? new ExecuteRequestSpan(httpContextActivity, context, options, enricher, false)
            : ExecuteRequestSpan.Start(Source, context, options, enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var activity = span.Activity;

            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(error);

            enricher.EnrichRequestError(context, error, activity);
        }
    }

    public override void RequestError(RequestContext context, IError error)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var activity = span.Activity;

            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddGraphQLError(error);

            enricher.EnrichRequestError(context, error, activity);
        }
    }

    public override IDisposable ParseDocument(RequestContext context)
    {
        if (options.SkipParseDocument)
        {
            return EmptyScope;
        }

        var span = ParsingSpan.Start(Source, context, enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ValidateDocument(RequestContext context)
    {
        if (options.SkipValidateDocument)
        {
            return EmptyScope;
        }

        var span = ValidationSpan.Start(Source, context, enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
        if (!context.Features.TryGet<ValidationSpan>(out var span))
        {
            return;
        }

        var activity = span.Activity;

        activity.SetStatus(ActivityStatusCode.Error);

        foreach (var error in errors)
        {
            activity.AddGraphQLError(error);
        }

        enricher.EnrichValidationErrors(context, errors, activity);
    }

    public override IDisposable PlanOperation(RequestContext context, string operationPlanId)
    {
        if (options.SkipPlanOperation)
        {
            return EmptyScope;
        }

        var span = PlanOperationSpan.Start(Source, context, enricher, operationPlanId);

        return span ?? EmptyScope;
    }

    public override IDisposable CoerceVariables(RequestContext context)
    {
        if (options.SkipCoerceVariables)
        {
            return EmptyScope;
        }

        if (context.GetOperationPlan() is not { } plan)
        {
            return EmptyScope;
        }

        var span = VariableCoercionSpan.Start(
            Source,
            context,
            plan.Operation.Definition.Operation,
            plan.OperationName,
            enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ExecuteOperation(RequestContext context)
    {
        if (options.SkipExecuteOperation)
        {
            return EmptyScope;
        }

        if (context.GetOperationPlan() is not { } plan)
        {
            return EmptyScope;
        }

        var span = ExecuteOperationSpan.Start(
            Source,
            context,
            plan.Operation.Definition.Operation,
            plan.OperationName,
            enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName)
        => ExecuteNode(context, node, schemaName);

    public override IDisposable ExecuteOperationBatchNode(
        OperationPlanContext context,
        OperationBatchExecutionNode node,
        string schemaName)
        => ExecuteNode(context, node, schemaName);

    public override IDisposable ExecuteNodeFieldNode(
        OperationPlanContext context,
        NodeFieldExecutionNode node)
        => ExecuteNode(context, node, null);

    public override IDisposable ExecuteIntrospectionNode(
        OperationPlanContext context,
        IntrospectionExecutionNode node)
        => ExecuteNode(context, node, null);

    public override void ExecutionNodeError(
        OperationPlanContext context,
        ExecutionNode node,
        Exception error)
    {
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(error);

            enricher.EnrichExecutionNodeError(context, node, error, activity);
        }
    }

    public override void SourceSchemaTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(error);

            enricher.EnrichSourceSchemaTransportError(context, node, schemaName, error, activity);
        }
    }

    public override void SourceSchemaStoreError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(error);

            enricher.EnrichSourceSchemaStoreError(context, node, schemaName, error, activity);
        }
    }

    public override IDisposable OnSubscriptionEvent(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        enricher.EnrichOnSubscriptionEvent(context, node, schemaName, subscriptionId, activity);

        return activity;
    }

    public override void RetrievedDocumentFromCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(RetrievedDocumentFromCache)));
        }
    }

    public override void RetrievedDocumentFromStorage(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(RetrievedDocumentFromStorage)));
        }
    }

    public override void AddedDocumentToCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedDocumentToCache)));
        }
    }

    public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedOperationPlanToCache)));
        }
    }

    private IDisposable ExecuteNode(OperationPlanContext context, ExecutionNode node, string? schemaName)
    {
        if (options.SkipExecutePlanNodes)
        {
            return EmptyScope;
        }

        var span = ExecutePlanNodeSpan.Start(Source, context, node, schemaName, enricher);

        return span ?? EmptyScope;
    }
}
