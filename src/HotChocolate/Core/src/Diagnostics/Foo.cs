using System;
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Diagnostics;

public class FooDiagnosticListener : ExecutionDiagnosticEventListener
{
    private readonly ActivitySource _activitySource = new ActivitySource("HotChocolate-Execution", GetVersion());


    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Activity? activity = _activitySource.CreateActivity("GraphQL-Request", ActivityKind.Server);

        if (activity is null)
        {
            return EmptyScope;
        }

        return new RequestEvent(context, activity);
    }

    private static string GetVersion()
        => typeof(FooDiagnosticListener).Assembly.GetName().Version.ToString();
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

    public void Dispose()
    {

        if (_context.Operation is not null)
        {
            if (_context.Operation.Name is not null)
            {
                _activity.SetTag("graphql.request.operation.name", _context.Operation.Name);
            }
            
            _activity.SetTag("graphql.request.operation.kind", _context.Operation.Type);
        }


        throw new NotImplementedException();
    }
}

