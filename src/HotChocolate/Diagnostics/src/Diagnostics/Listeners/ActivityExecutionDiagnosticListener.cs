using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Diagnostics.Scopes;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Diagnostics.ContextKeys;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics;

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

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = Source.StartActivity();

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
            ((Activity)activity!).AddEvent(new(nameof(RetrievedDocumentFromCache)));
        }
    }

    public override void RetrievedDocumentFromStorage(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            ((Activity)activity!).AddEvent(new(nameof(RetrievedDocumentFromStorage)));
        }
    }

    public override void AddedDocumentToCache(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            ((Activity)activity!).AddEvent(new(nameof(AddedDocumentToCache)));
        }
    }

    public override void AddedOperationToCache(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(RequestActivity, out var activity))
        {
            ((Activity)activity!).AddEvent(new(nameof(AddedOperationToCache)));
        }
    }

    public override IDisposable ParseDocument(IRequestContext context)
    {
        if (_options.SkipParseDocument)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ParserActivity] = activity;

        return new ParseDocumentScope(_enricher, context, activity);
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        if (context.ContextData.TryGetValue(ParserActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            _enricher.EnrichSyntaxError(context, (Activity)activity, error);
        }
    }

    public override IDisposable ValidateDocument(IRequestContext context)
    {
        if (_options.SkipValidateDocument)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

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

            foreach (IError error in errors)
            {
                _enricher.EnrichValidationError(context, activity, error);
            }
        }
    }

    public override IDisposable AnalyzeOperationComplexity(IRequestContext context)
    {
        if (_options.SkipAnalyzeOperationComplexity)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ComplexityActivity] = activity;

        return new AnalyzeOperationComplexityScope(_enricher, context, activity);
    }

    public override void OperationComplexityAnalyzerCompiled(IRequestContext context)
    {
        if (context.ContextData.TryGetValue(ComplexityActivity, out var activity))
        {
            ((Activity)activity!).AddEvent(new(nameof(OperationComplexityAnalyzerCompiled)));
        }
    }

    public override void OperationComplexityResult(
        IRequestContext context,
        int complexity,
        int allowedComplexity)
    {
        if (context.ContextData.TryGetValue(ComplexityActivity, out var activity))
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new(nameof(complexity), complexity),
                new(nameof(allowedComplexity), allowedComplexity),
                new("allowed", allowedComplexity >= complexity)
            };
            ((Activity)activity!).AddEvent(new(nameof(OperationComplexityResult), tags: new(tags)));
        }
    }

    public override IDisposable CoerceVariables(IRequestContext context)
    {
        if (_options.SkipCoerceVariables)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

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

        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new CompileOperationScope(_enricher, context, activity);
    }

    public override IDisposable BuildQueryPlan(IRequestContext context)
    {
        if (_options.SkipBuildQueryPlan)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new BuildQueryPlanScope(_enricher, context, activity);
    }

    public override IDisposable ExecuteOperation(IRequestContext context)
    {
        if (_options.SkipExecuteOperation)
        {
            return EmptyScope;
        }

        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable ExecuteStream(IRequestContext context)
    {
        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable ExecuteSubscription(ISubscription subscription)
    {
        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable OnSubscriptionEvent(SubscriptionEventContext subscription)
    {
        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return activity;
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        Activity? activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichResolveFieldValue(context, activity);
        context.SetLocalValue(ResolverActivity, activity);

        return activity!;
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        if (context.ContextData.TryGetValue(ResolverActivity, out var activity))
        {
            Debug.Assert(activity is not null, "The activity mustn't be null!");
            _enricher.EnrichResolverError(context, error, (Activity)activity);
        }
    }
}
