using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class CompileOperationScope : RequestScopeBase
{
    public CompileOperationScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichCompileOperation(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.Operation is not null)
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
