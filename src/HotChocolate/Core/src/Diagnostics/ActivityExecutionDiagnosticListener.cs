using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Diagnostics.Scopes;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using static HotChocolate.Diagnostics.ContextKeys;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics;

public class ActivityExecutionDiagnosticListener : ExecutionDiagnosticEventListener
{
    private readonly ActivityEnricher _enricher;

    public ActivityExecutionDiagnosticListener(ActivityEnricher activityEnricher)
        => _enricher = activityEnricher;

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = Source.StartActivity(ActivityNames.ExecuteRequest);

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
        Activity? activity = Source.StartActivity(ActivityNames.ParseDocument);

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
            var tags = new List<KeyValuePair<string, object?>>();
            _enricher.EnrichSyntaxError(context, error, tags);
            ((Activity)activity!).AddEvent(new(nameof(SyntaxError), tags: new(tags)));
        }
    }

    public override IDisposable ValidateDocument(IRequestContext context)
    {
        Activity? activity = Source.StartActivity("GraphQL-Validate-Document");

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ValidateActivity] = activity;

        return new ValidateDocumentScope(_enricher, context, activity);
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        if (context.ContextData.TryGetValue(ValidateActivity, out var activity))
        {
            foreach (IError error in errors)
            {
                var tags = new List<KeyValuePair<string, object?>>();
                _enricher.EnrichValidationErrors(context, error, tags);
                ((Activity)activity!).AddEvent(new("ValidationError", tags: new(tags)));
            }
        }
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        Activity? activity = Source.StartActivity(ActivityNames.ResolveFieldValue);

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichResolver(context, activity);
        context.SetLocalValue(ResolverActivity, activity);

        return activity!;
    }
}
