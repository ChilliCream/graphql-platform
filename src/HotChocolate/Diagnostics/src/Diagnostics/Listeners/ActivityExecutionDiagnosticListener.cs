using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityExecutionDiagnosticListener : ExecutionDiagnosticEventListener
{
    private const string ResolveFieldSpanKey = "HotChocolate.Diagnostics.ResolveFieldSpan";

    private static readonly AsyncLocal<SubscriptionEventSpan?> s_currentSubscriptionEventSpan = new();
    private readonly ActivityEnricher _enricher;
    private readonly InstrumentationOptions _options;

    public ActivityExecutionDiagnosticListener(
        ActivityEnricher enricher,
        InstrumentationOptions options)
    {
        _enricher = enricher;
        _options = options;
    }

    public override bool EnableResolveFieldValue => _options.EnableResolveFieldValue;

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        Activity? httpContextActivity = null;

        if (_options.SkipExecuteRequest)
        {
            if (_options.SkipExecuteHttpRequest
                || !context.Features.TryGet<HttpContext>(out var httpContext))
            {
                return EmptyScope;
            }

            if (httpContext.Features.Get<ExecuteHttpRequestSpan>() is { IsBatch: false } httpRequestSpan)
            {
                httpContextActivity = httpRequestSpan.Activity;
            }
        }

        var span = httpContextActivity is not null
            ? new ExecuteRequestSpan(httpContextActivity, context, _options, _enricher, false)
            : ExecuteRequestSpan.Start(Source, context, _options, _enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        // An intentional caller cancellation (browser tab closed, connection
        // dropped) surfaces here as an OperationCanceledException. Per the
        // OpenTelemetry semantic conventions this is not an error, so the span
        // is left Unset with no error.type and no exception event. Server-side
        // execution timeouts never reach RequestError as an exception (the
        // timeout middleware turns them into an HC0045 result), so only genuine
        // client cancellations are filtered out here.
        if (error is OperationCanceledException)
        {
            return;
        }

        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var activity = span.Activity;

            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(error);
            activity.SetErrorType(error);

            _enricher.EnrichRequestError(context, error, activity);
        }
    }

    public override void RequestError(RequestContext context, IError error)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            var activity = span.Activity;

            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetErrorType(error, ActivityExtensions.ExecutionErrorType);

            _enricher.EnrichRequestError(context, error, activity);
        }
    }

    public override IDisposable ParseDocument(RequestContext context)
    {
        if (_options.SkipParseDocument)
        {
            return EmptyScope;
        }

        var span = ParsingSpan.Start(Source, context, _enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ValidateDocument(RequestContext context)
    {
        if (_options.SkipValidateDocument)
        {
            return EmptyScope;
        }

        var span = ValidationSpan.Start(Source, context, _enricher);

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

        if (errors is [var firstError, ..])
        {
            activity.SetErrorType(firstError, ActivityExtensions.ValidationErrorType);

            // Propagate the phase-specific error.type to the root request span so
            // it does not fall back to EXECUTION_ERROR when validation produced
            // the failure. SetErrorType is a no-op if the tag is already set.
            if (context.Features.TryGet<ExecuteRequestSpan>(out var rootSpan))
            {
                rootSpan.Activity.SetErrorType(firstError, ActivityExtensions.ValidationErrorType);
            }
        }

        _enricher.EnrichValidationErrors(context, errors, activity);
    }

    public override IDisposable AnalyzeOperationCost(RequestContext context)
    {
        if (_options.SkipAnalyzeComplexity)
        {
            return EmptyScope;
        }

        var span = AnalyzeOperationComplexitySpan.Start(Source, context, _enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void OperationCost(RequestContext context, double fieldCost, double typeCost)
    {
        if (!context.Features.TryGet<AnalyzeOperationComplexitySpan>(out var span))
        {
            return;
        }

        span.SetCost(fieldCost, typeCost);
    }

    public override IDisposable CompileOperation(RequestContext context)
    {
        if (_options.SkipCompileOperation)
        {
            return EmptyScope;
        }

        var span = CompileOperationSpan.Start(Source, context, _enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable CoerceVariables(RequestContext context)
    {
        if (_options.SkipCoerceVariables)
        {
            return EmptyScope;
        }

        OperationType operationType;
        string? operationName;

        if (context.TryGetOperation(out var operation))
        {
            operationType = operation.Kind;
            operationName = operation.Name;
        }
        else if (context.OperationDocumentInfo.NormalizedDocument
            is { Definitions: [OperationDefinitionNode normalizedOperation] })
        {
            // Variable coercion now runs before the operation is compiled, so the compiled
            // operation is not available yet; the normalized document carries the same
            // operation type and name.
            operationType = normalizedOperation.Operation;
            operationName = normalizedOperation.Name?.Value;
        }
        else
        {
            return EmptyScope;
        }

        var span = VariableCoercionSpan.Start(
            Source,
            context,
            operationType,
            operationName,
            _enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ExecuteOperation(RequestContext context)
    {
        if (_options.SkipExecuteOperation)
        {
            return EmptyScope;
        }

        if (!context.TryGetOperation(out var operation))
        {
            return EmptyScope;
        }

        var span = ExecuteOperationSpan.Start(
            Source,
            context,
            operation.Kind,
            operation.Name,
            _enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        if (_options.SkipResolveFieldValue)
        {
            return EmptyScope;
        }

        var span = ResolveFieldSpan.Start(Source, context, _enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.LocalContextData = context.LocalContextData.SetItem(ResolveFieldSpanKey, span);

        return span;
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        if (context.LocalContextData.TryGetValue(ResolveFieldSpanKey, out var value)
            && value is ResolveFieldSpan span)
        {
            span.Activity.SetStatus(ActivityStatusCode.Error);
            span.Activity.SetErrorType(
                error,
                ActivityExtensions.ExecutionErrorType,
                preferException: true);

            _enricher.EnrichResolverError(context, error, span.Activity);
        }

        // For subscription operations, the per-event errors are not visible to
        // ExecuteRequestSpanBase.OnComplete (each event is its own result). Emit
        // the graphql.error event on the subscription event span (the effective
        // root for the event) so the error surfaces there too.
        if (s_currentSubscriptionEventSpan.Value is { } eventSpan)
        {
            var eventActivity = eventSpan.Activity;
            eventActivity.SetStatus(ActivityStatusCode.Error);
            eventActivity.SetErrorType(error, ActivityExtensions.ExecutionErrorType);
            eventActivity.AddGraphQLErrorEvent(
                error,
                operationType: SemanticConventions.GraphQL.Operation.TypeValues[
                    context.Operation.Definition.Operation],
                operationName: context.Operation.Name,
                schemaCoordinate: context.Selection.Field.Coordinate.ToString(),
                documentInfo: context.Features.Get<OperationDocumentInfo>());
        }
    }

    public override IDisposable ExecuteSubscription(
        RequestContext context,
        ulong subscriptionId)
    {
        if (Activity.Current is not { } currentActivity)
        {
            return EmptyScope;
        }

        context.Features.Set(
            new SubscriptionContextFeature
            {
                SubscriptionContext = currentActivity.Context
            });

        return EmptyScope;
    }

    public override IDisposable OnSubscriptionEvent(RequestContext context, ulong subscriptionId)
    {
        ActivityContext? subscriptionContext = null;

        if (context.Features.TryGet<SubscriptionContextFeature>(out var feature)
            && feature.SubscriptionContext is { } storedSubscriptionContext)
        {
            subscriptionContext = storedSubscriptionContext;
        }

        var span = SubscriptionEventSpan.Start(
            Source,
            context,
            context.TryGetOperation(out var operation) ? operation.Name : null,
            subscriptionId,
            subscriptionContext);

        if (span is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichOnSubscriptionEvent(context, subscriptionId, span.Activity);

        s_currentSubscriptionEventSpan.Value = span;

        return span;
    }

    public override void SubscriptionEventError(
        RequestContext context,
        ulong subscriptionId,
        Exception exception)
    {
        // A subscription event can be cancelled for two very different reasons:
        // the caller intentionally dropped the connection (client abort) or the
        // event exceeded its server-side execution budget (per-event timeout).
        // Only the latter is an error.
        //
        // Both surface as an OperationCanceledException, but they differ in the
        // token that fired: a client abort cancels the request itself
        // (RequestAborted), whereas a per-event timeout cancels an internal,
        // per-event source while leaving the request abort untouched. Crucially,
        // the request-level timeout token is released once the subscription
        // stream is established, so RequestAborted only ever signals a genuine
        // caller cancellation for a running subscription. We therefore treat a
        // cancellation as a client abort only when the request was aborted,
        // leaving the span Unset per the OpenTelemetry semantic conventions.
        if (exception is OperationCanceledException
            && context.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(exception);
            activity.SetErrorType(exception);
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
            _enricher.EnrichDocumentNotFoundInStorage(context, documentId, span.Activity);
        }
    }

    public override void UntrustedDocumentRejected(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(UntrustedDocumentRejected)));
            _enricher.EnrichUntrustedDocumentRejected(context, span.Activity);
        }
    }

    public override void AddedDocumentToCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedDocumentToCache)));
            _enricher.EnrichAddedDocumentToCache(context, span.Activity);
        }
    }

    public override void AddedOperationToCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedOperationToCache)));
            _enricher.EnrichAddedOperationToCache(context, span.Activity);
        }
    }

    private sealed class SubscriptionContextFeature
    {
        public ActivityContext? SubscriptionContext { get; set; }
    }
}
