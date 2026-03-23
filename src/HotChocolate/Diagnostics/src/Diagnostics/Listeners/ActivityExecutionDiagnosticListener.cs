using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityExecutionDiagnosticListener(
    ActivityEnricher enricher,
    InstrumentationOptions options) : ExecutionDiagnosticEventListener
{
    private const string ResolveFieldSpanKey = "HotChocolate.Diagnostics.ResolveFieldSpan";

    public override bool EnableResolveFieldValue => true;

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

    public override IDisposable AnalyzeOperationCost(RequestContext context)
    {
        if (options.SkipAnalyzeComplexity)
        {
            return EmptyScope;
        }

        var span = AnalyzeOperationComplexitySpan.Start(Source, context, enricher);

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
        if (options.SkipCompileOperation)
        {
            return EmptyScope;
        }

        var span = CompileOperationSpan.Start(Source, context, enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable CoerceVariables(RequestContext context)
    {
        if (options.SkipCoerceVariables)
        {
            return EmptyScope;
        }

        if (!context.TryGetOperation(out var operation))
        {
            return EmptyScope;
        }

        var span = VariableCoercionSpan.Start(
            Source,
            context,
            operation.Kind,
            operation.Name,
            enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ExecuteOperation(RequestContext context)
    {
        if (options.SkipExecuteOperation)
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
            enricher);

        return span ?? EmptyScope;
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        if (options.SkipResolveFieldValue)
        {
            return EmptyScope;
        }

        var span = ResolveFieldSpan.Start(Source, context, enricher);

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
            span.Activity.AddGraphQLError(error);

            enricher.EnrichResolverError(context, error, span.Activity);
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

        enricher.EnrichOnSubscriptionEvent(context, subscriptionId, span.Activity);

        return span;
    }

    public override void SubscriptionEventError(
        RequestContext context,
        ulong subscriptionId,
        Exception exception)
    {
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.AddException(exception);
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
            span.Activity.AddEvent(new(nameof(DocumentNotFoundInStorage)));
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
        }
    }

    public override void AddedOperationToCache(RequestContext context)
    {
        if (context.Features.TryGet<ExecuteRequestSpan>(out var span))
        {
            span.Activity.AddEvent(new(nameof(AddedOperationToCache)));
        }
    }

    private sealed class SubscriptionContextFeature
    {
        public ActivityContext? SubscriptionContext { get; set; }
    }
}
