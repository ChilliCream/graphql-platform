using System;
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace HotChocolate.Diagnostics;

public class ActivityExecutionDiagnosticListener : ExecutionDiagnosticEventListener
{
    private readonly ActivitySource _activitySource = new ActivitySource(GetName(), GetVersion());

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = _activitySource.StartActivity("GraphQL-Request");

        if (activity is null)
        {
            return EmptyScope;
        }

        return new RequestEvent(context, activity);
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        Activity? activity = _activitySource.StartActivity(context.ResponseName);

        if (activity is null)
        {
            return EmptyScope;
        }

        activity.DisplayName = $"Field: {context.Path}";
        activity.SetTag("graphql.selection.name", context.Selection.ResponseName);
        activity.SetTag("graphql.selection.field", context.Selection.Field.Name);
        activity.SetTag("graphql.selection.path", context.Path.ToString());

        return activity;
    }

    private static string GetName()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Version!.ToString();
}

internal class RequestEvent : IDisposable
{
    private readonly IRequestContext _context;
    private Activity _activity;

    public RequestEvent(IRequestContext context, Activity activity)
    {
        _context = context;
        _activity = activity;
    }

    private void EnrichActivity()
    {
        if (_context.Operation is not null)
        {
            _activity.SetTag("graphql.request.operation.id", _context.OperationId);
            _activity.SetTag("graphql.request.operation.kind", _context.Operation.Type);

            if (_context.Operation.Name is not null)
            {
                _activity.SetTag("graphql.request.operation.name", _context.Operation.Name);
            }
        }

        if (_context.Result is IQueryResult result)
        {
            _activity.SetTag("graphql.request.errors.count", result.Errors?.Count ?? 0);
        }
    }

    public void Dispose()
    {
        EnrichActivity();
        _activity.Dispose();
    }
}
