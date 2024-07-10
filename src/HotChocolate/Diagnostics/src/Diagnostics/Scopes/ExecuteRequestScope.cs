using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class ExecuteRequestScope : RequestScopeBase
{
    public ExecuteRequestScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichExecuteRequest(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.Result is null or IOperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(Status.Error);
            Activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
