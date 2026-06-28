using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Fusion.Diagnostics.HotChocolateFusionActivitySource;

namespace HotChocolate.Fusion.Diagnostics.Listeners;

internal sealed class FusionActivityExecutionDiagnosticEventListener(
    FusionActivityEnricher enricher,
    InstrumentationOptions options)
    : FusionExecutionDiagnosticEventListener
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
            activity.SetErrorType(error);

            enricher.EnrichRequestError(context, error, activity);
        }
    }

    public override void RequestError(RequestContext context, IError error)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var activity = span.Activity;

            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetErrorType(error, ActivityExtensions.ExecutionErrorType);

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
            activity.SetErrorType(error, ActivityExtensions.ValidationErrorType);
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

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
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

    public override IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
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
            activity.AddGraphQLErrorEvent(
                error,
                operationType: GetOperationType(context),
                operationName: context.OperationPlan.Operation.Name,
                documentInfo: context.RequestContext.OperationDocumentInfo);
            activity.SetErrorType(error);

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
            activity.AddGraphQLErrorEvent(
                error,
                operationType: GetOperationType(context),
                operationName: context.OperationPlan.Operation.Name,
                documentInfo: context.RequestContext.OperationDocumentInfo);
            activity.SetErrorType(error);

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
            activity.AddGraphQLErrorEvent(
                error,
                operationType: GetOperationType(context),
                operationName: context.OperationPlan.Operation.Name,
                documentInfo: context.RequestContext.OperationDocumentInfo);
            activity.SetErrorType(error);

            enricher.EnrichSourceSchemaStoreError(context, node, schemaName, error, activity);
        }
    }

    public override IDisposable OnSubscriptionEvent(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        var subscriptionContext = context.RequestContext.Features.TryGet<ExecuteRequestSpan>(out var requestSpan)
            ? requestSpan.Activity.Context
            : (ActivityContext?)null;

        var span = SubscriptionEventSpan.Start(
            Source,
            context.RequestContext,
            context.OperationPlan.Operation.Name,
            subscriptionId,
            subscriptionContext);

        if (span is null)
        {
            return EmptyScope;
        }

        enricher.EnrichOnSubscriptionEvent(context, node, schemaName, subscriptionId, span.Activity);

        return span;
    }

    public override void SubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception)
    {
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddGraphQLErrorEvent(
                exception,
                operationType: GetOperationType(context),
                operationName: context.OperationPlan.Operation.Name,
                documentInfo: context.RequestContext.OperationDocumentInfo);
            activity.SetErrorType(exception);

            enricher.EnrichSubscriptionEventError(
                context,
                node,
                schemaName,
                subscriptionId,
                exception,
                activity);
        }
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

    public override void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var tags = new ActivityTagsCollection();

            if (documentId.HasValue)
            {
                tags[SemanticConventions.GraphQL.Document.Id] = documentId.Value;
            }

            span.Activity.AddEvent(new ActivityEvent(nameof(DocumentNotFoundInStorage), default, tags));
            enricher.EnrichDocumentNotFoundInStorage(context, documentId, span.Activity);
        }
    }

    public override void UntrustedDocumentRejected(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(UntrustedDocumentRejected)));
            enricher.EnrichUntrustedDocumentRejected(context, span.Activity);
        }
    }

    public override void AddedDocumentToCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedDocumentToCache)));
            enricher.EnrichAddedDocumentToCache(context, span.Activity);
        }
    }

    public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedOperationPlanToCache)));
            enricher.EnrichAddedOperationPlanToCache(context, operationPlanId, span.Activity);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetOperationType(OperationPlanContext context)
        => SemanticConventions.GraphQL.Operation.TypeValues[
            context.OperationPlan.Operation.Definition.Operation];
}
