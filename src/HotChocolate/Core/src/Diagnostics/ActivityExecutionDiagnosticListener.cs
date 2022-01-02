using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using static HotChocolate.Diagnostics.ContextKeys;

namespace HotChocolate.Diagnostics;

public class ActivityExecutionDiagnosticListener : ExecutionDiagnosticEventListener
{
    private readonly ActivitySource _activitySource = new ActivitySource(GetName(), GetVersion());
    private readonly IDiagnosticActivityEnricher _enricher;

    public ActivityExecutionDiagnosticListener(IDiagnosticActivityEnricher enricher)
    {
        Console.WriteLine(GetName());
        _enricher = enricher;
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = _activitySource.StartActivity("GraphQL-Execute-Request");

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[RequestActivity] = activity;

        return new RequestEvent(_enricher, context, activity);
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
        Activity? activity = _activitySource.StartActivity("GraphQL-Parse-Document");

        if (activity is null)
        {
            return EmptyScope;
        }

        context.ContextData[ParserActivity] = activity;
        _enricher.EnrichParseDocument(context, activity);

        return activity;
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
        Activity? activity = _activitySource.StartActivity("GraphQL-Validate-Document");

        if (activity is null)
        {
            return EmptyScope;
        }

        return new ValidateEvent(_enricher, context, activity);
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        Activity? activity = _activitySource.StartActivity("GraphQL-Execute-Resolver");

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichResolver(context, activity);
        context.SetLocalValue("Activity", activity);

        return activity!;
    }

    private static string GetName()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Version!.ToString();
}

internal sealed class RequestEvent : IDisposable
{
    private readonly IDiagnosticActivityEnricher _enricher;
    private readonly IRequestContext _context;
    private readonly Activity _activity;

    public RequestEvent(IDiagnosticActivityEnricher enricher, IRequestContext context, Activity activity)
    {
        _enricher = enricher;
        _context = context;
        _activity = activity;
    }

    public void Dispose()
    {
        _enricher.EnrichRequest(_context, _activity);
        _activity.Dispose();
    }
}

internal sealed class ValidateEvent : IDisposable
{
    private readonly IDiagnosticActivityEnricher _enricher;
    private readonly IRequestContext _context;
    private readonly Activity _activity;

    public ValidateEvent(IDiagnosticActivityEnricher enricher, IRequestContext context, Activity activity)
    {
        _enricher = enricher;
        _context = context;
        _activity = activity;
    }

    public void Dispose()
    {
        _enricher.EnrichValidateDocument(_context, _activity);
        _activity.Dispose();
    }
}

public interface IDiagnosticActivityEnricher
{
    void EnrichRequest(IRequestContext context, Activity activity);

    void EnrichValidateDocument(IRequestContext context, Activity activity);

    void EnrichParseDocument(IRequestContext context, Activity activity);

    void EnrichSyntaxError(IRequestContext context, IError error, ICollection<KeyValuePair<string, object?>> tags);

    void EnrichResolver(IMiddlewareContext context, Activity activity);
}

public class DefaultDiagnosticActivityEnricher : IDiagnosticActivityEnricher
{
    public virtual void EnrichRequest(IRequestContext context, Activity activity)
    {
        if (context.Operation is not null)
        {
            activity.SetTag("graphql.request.document.id", context.DocumentId);
            activity.SetTag("graphql.request.document.hash", context.DocumentHash);
            activity.SetTag("graphql.request.document.valid", context.IsValidDocument);

            activity.SetTag("graphql.request.operation.id", context.OperationId);
            activity.SetTag("graphql.request.operation.kind", context.Operation.Type);

            if (context.Operation.Name is not null)
            {
                activity.DisplayName = context.Operation.Name;
                activity.SetTag("graphql.request.operation.name", context.Operation.Name);
            }
        }

        if (context.Result is IQueryResult result)
        {
            activity.SetTag("graphql.request.errors.count", result.Errors?.Count ?? 0);
            activity.SetTag("graphql.request.status", (result.Errors?.Count ?? 0) == 0 ? "success" : "error");
        }
    }

    public virtual void EnrichValidateDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate";
        activity.SetTag("graphql.request.document.id", context.DocumentId);
        activity.SetTag("graphql.request.document.hash", context.DocumentHash);
        activity.SetTag("graphql.request.document.valid", context.IsValidDocument);
    }

    public virtual void EnrichParseDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse";
    }

    public virtual void EnrichSyntaxError(IRequestContext context, IError error, ICollection<KeyValuePair<string, object?>> tags)
    {
        tags.Add(new("graphql.error.message", error.Message));
        tags.Add(new("graphql.error.code", error.Code));

        if (error.Locations is { Count: > 0 })
        {
            for (int i = 0; i < error.Locations.Count; i++)
            {
                tags.Add(new($"graphql.error.location.{i}.column", error.Locations[i].Column));
                tags.Add(new($"graphql.error.location.{i}.line", error.Locations[i].Line));
            }
        }
    }

    public virtual void EnrichResolver(IMiddlewareContext context, Activity activity)
    {
        activity.DisplayName = $"{context.Path}";
        activity.SetTag("graphql.selection.name", context.Selection.ResponseName);
        activity.SetTag("graphql.selection.coordinate", context.Selection.Field.Coordinate.ToString());
        activity.SetTag("graphql.selection.type", context.Selection.Field.Type.Print());
        activity.SetTag("graphql.selection.path", context.Path.ToString());
    }
}

internal static class ContextKeys
{
    public const string RequestActivity = "HotChocolate.Diagnostics.Request";
    public const string ParserActivity = "HotChocolate.Diagnostics.Parser";
    public const string ResolverActivity = "HotChocolate.Diagnostics.Resolver";
}
