using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class BuildQueryPlanScope : RequestScopeBase
{
    public BuildQueryPlanScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichBuildQueryPlan(Context, Activity);
}
