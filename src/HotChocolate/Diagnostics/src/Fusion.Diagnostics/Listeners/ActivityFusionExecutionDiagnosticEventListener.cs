using System.Diagnostics;
using HotChocolate.Fusion.Diagnostics.Scopes;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;
using static HotChocolate.Fusion.Diagnostics.ContextKeys;
using static HotChocolate.Fusion.Diagnostics.HotChocolateFusionActivitySource;

namespace HotChocolate.Fusion.Diagnostics.Listeners;

// TODO: Add more items
// TODO: Check if AddedOperationPlanToCache is correct
internal sealed class ActivityFusionExecutionDiagnosticEventListener : FusionExecutionDiagnosticEventListener
{
    private readonly InstrumentationOptions _options;
    private readonly FusionActivityEnricher _enricher;

    public ActivityFusionExecutionDiagnosticEventListener(
        FusionActivityEnricher enricher,
        InstrumentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(enricher);
        ArgumentNullException.ThrowIfNull(options);

        _enricher = enricher;
        _options = options;
    }

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        Activity? activity = null;

        if (_options.SkipExecuteRequest)
        {
            if (!_options.SkipExecuteHttpRequest
                && context.ContextData.TryGetValue(nameof(HttpContext), out var value)
                && value is HttpContext httpContext
                && httpContext.Items.TryGetValue(HttpRequestActivity, out value)
                && value is not null)
            {
                activity = (Activity)value;
            }
            else
            {
                return EmptyScope;
            }
        }

        activity ??= Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[RequestActivity] = activity;

        return new ExecuteRequestScope(_enricher, context, activity);
    }

    public override void RetrievedDocumentFromCache(RequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(RetrievedDocumentFromCache)));
        }
    }

    public override void RetrievedDocumentFromStorage(RequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(RetrievedDocumentFromStorage)));
        }
    }

    public override void AddedDocumentToCache(RequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(AddedDocumentToCache)));
        }
    }

    public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(AddedOperationPlanToCache)));
        }
    }

    public override IDisposable ParseDocument(RequestContext context)
    {
        if (_options.SkipParseDocument)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[RequestActivity] = activity;

        return new ParseDocumentScope(_enricher, context, activity);
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;
            _enricher.EnrichRequestError(context, activity, error);
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override void RequestError(RequestContext context, IError error)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;
            _enricher.EnrichRequestError(context, activity, error);
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
        if (context.ContextData.TryGetValue(ValidateActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;

            foreach (var error in errors)
            {
                _enricher.EnrichValidationError(context, activity, error);
            }

            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override IDisposable ValidateDocument(RequestContext context)
    {
        if (_options.SkipValidateDocument)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ValidateActivity] = activity;

        return new ValidateDocumentScope(_enricher, context, activity);
    }

    public override IDisposable CoerceVariables(RequestContext context)
    {
        if (_options.SkipCoerceVariables)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new CoerceVariablesScope(_enricher, context, activity);
    }

    public override IDisposable PlanOperation(RequestContext context, string operationPlanId)
    {
        if (_options.SkipPlanOperation)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new PlanOperationScope(_enricher, context, activity);
    }

    public override IDisposable ExecuteOperation(RequestContext context)
    {
        if (_options.SkipExecuteOperation)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new ExecuteOperationScope(_enricher, context, activity);
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

        return activity;
    }
}
