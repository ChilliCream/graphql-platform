using System.Diagnostics;
using HotChocolate.Diagnostics.Scopes;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
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

    public override IDisposable ExecuteRequest(RequestContext context)
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

    public override void AddedOperationToCache(RequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            ((Activity)activity).AddEvent(new(nameof(AddedOperationToCache)));
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

        context.ContextData[ParserActivity] = activity;

        return new ParseDocumentScope(_enricher, context, activity);
    }

    public override void ExecutionError(
        RequestContext context,
        ErrorKind kind,
        IReadOnlyList<IError> errors,
        object? state)
    {
        switch (kind)
        {
            case ErrorKind.SyntaxError:
                {
                    foreach (var error in errors)
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
                }
                break;

            case ErrorKind.ValidationError:
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
                break;

            case ErrorKind.FieldError:
                {
                    if (!_options.SkipResolveFieldValue)
                    {
                        if (state is IMiddlewareContext middlewareContext
                            && middlewareContext.LocalContextData.TryGetValue(ResolverActivity, out var localValue)
                            && localValue is Activity activity)
                        {
                            foreach (var error in errors)
                            {
                                _enricher.EnrichResolverError(context, middlewareContext, error, activity);
                            }

                            activity.SetStatus(Status.Error);
                            activity.SetStatus(ActivityStatusCode.Error);
                        }
                        else if (context.ContextData.TryGetValue(RequestActivity, out var value))
                        {
                            Debug.Assert(value is not null, "The activity mustn't be null!");

                            activity = (Activity)value;

                            foreach (var error in errors)
                            {
                                _enricher.EnrichResolverError(context, null, error, activity);
                            }

                            activity.SetStatus(Status.Error);
                            activity.SetStatus(ActivityStatusCode.Error);
                        }
                    }
                }
                break;
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

    public override IDisposable AnalyzeOperationCost(RequestContext context)
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

    public override void OperationCost(RequestContext context, double fieldCost, double typeCost)
    {
        if (context.ContextData.TryGetValue(ComplexityActivity, out var value))
        {
            Debug.Assert(value is not null, "The activity mustn't be null!");

            var activity = (Activity)value;

            var documentInfo = context.OperationDocumentInfo;
            activity.SetTag("graphql.operation.id", documentInfo.Id.Value);
            activity.SetTag("graphql.operation.fieldCost", fieldCost);
            activity.SetTag("graphql.operation.typeCost", typeCost);
        }
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

    public override IDisposable CompileOperation(RequestContext context)
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

    public override IDisposable ExecuteStream(IOperation operation)
    {
        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable OnSubscriptionEvent(RequestContext context)
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
}
