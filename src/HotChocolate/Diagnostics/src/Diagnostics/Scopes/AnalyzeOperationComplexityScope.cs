using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class AnalyzeOperationComplexityScope : RequestScopeBase
{
    public AnalyzeOperationComplexityScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichAnalyzeOperationComplexity(Context, Activity);

    protected override void SetStatus()
    {
        if(Context.ContextData.ContainsKey(OperationComplexityAllowed))
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
