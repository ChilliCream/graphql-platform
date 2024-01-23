using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class ExecuteOperationScope : RequestScopeBase
{
    public ExecuteOperationScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichExecuteOperation(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.Result is not null and not IQueryResult { Data: null, })
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            Activity.SetStatus(Status.Error);
            Activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
