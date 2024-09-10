using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.Diagnostics.Scopes;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.ContextKeys;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityExecutionDiagnosticListener : ExecutionDiagnosticEventListener
{
    private readonly InstrumentationOptions _options;
    private readonly ActivityEnricher _enricher;

    public ActivityExecutionDiagnosticListener(
        ActivityEnricher enricher,
        InstrumentationOptions options)
    {
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override bool EnableResolveFieldValue => true;

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = null;

        if (_options.SkipExecuteRequest)
        {
            if (!_options.SkipExecuteHttpRequest &&
                context.ContextData.TryGetValue(nameof(HttpContext), out var value) &&
                value is HttpContext httpContext &&
                httpContext.Items.TryGetValue(HttpRequestActivity, out value) &&
                value is not null)
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

    public override void RetrievedDocumentFromCache(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(RetrievedDocumentFromCache)));
        }
    }

    public override void RetrievedDocumentFromStorage(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(RetrievedDocumentFromStorage)));
        }
    }

    public override void AddedDocumentToCache(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(AddedDocumentToCache)));
        }
    }

    public override void AddedOperationToCache(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(AddedOperationToCache)));
        }
    }

    public override IDisposable ParseDocument(IRequestContext context)
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

        context.ContextData[ParserActivity] = activity;

        return new ParseDocumentScope(_enricher, context, activity);
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        if (context.ContextData.TryGetValue(ParserActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;
            _enricher.EnrichSyntaxError(context, activity, error);
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override IDisposable ValidateDocument(IRequestContext context)
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

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
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

    public override IDisposable AnalyzeOperationCost(IRequestContext context)
    {
        if (_options.SkipAnalyzeComplexity)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ComplexityActivity] = activity;

        return new AnalyzeOperationComplexityScope(_enricher, context, activity);
    }

    public override void OperationCost(IRequestContext context, double fieldCost, double typeCost)
    {
        if (context.ContextData.TryGetValue(ComplexityActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;

            activity.SetTag("graphql.operation.id", context.DocumentId?.Value);
            activity.SetTag("graphql.operation.fieldCost", fieldCost);
            activity.SetTag("graphql.operation.typeCost", typeCost);
        }
    }

    public override IDisposable CoerceVariables(IRequestContext context)
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

    public override IDisposable CompileOperation(IRequestContext context)
    {
        if (_options.SkipCompileOperation)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new CompileOperationScope(_enricher, context, activity);
    }

    public override IDisposable ExecuteOperation(IRequestContext context)
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

    public override IDisposable ExecuteStream(IOperation operation)
    {
        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    // Note: we removed public override IDisposable ExecuteSubscription(ISubscription subscription)
    // for now.

    public override IDisposable OnSubscriptionEvent(SubscriptionEventContext subscription)
    {
        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        if (_options.SkipResolveFieldValue)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichResolveFieldValue(context, activity);
        activity.SetStatus(Status.Ok);
        activity.SetStatus(ActivityStatusCode.Ok);

        context.SetLocalState(ResolverActivity, activity);

        return activity;
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        if (!_options.SkipResolveFieldValue &&
            context.LocalContextData.TryGetValue(ResolverActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;
            _enricher.EnrichResolverError(context, error, activity);
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
        if (!_options.SkipResolveFieldValue &&
            context.ContextData.TryGetValue(RequestActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;
            _enricher.EnrichResolverError(context, selection, error, activity);
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
