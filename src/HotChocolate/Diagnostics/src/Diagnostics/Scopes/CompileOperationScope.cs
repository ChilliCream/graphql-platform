using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class CompileOperationScope : RequestScopeBase
{
    public CompileOperationScope(
        ActivityEnricher enricher,
        RequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichCompileOperation(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.TryGetOperation(out _))
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
