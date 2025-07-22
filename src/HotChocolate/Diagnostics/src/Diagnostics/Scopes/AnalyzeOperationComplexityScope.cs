using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class AnalyzeOperationComplexityScope : RequestScopeBase
{
    public AnalyzeOperationComplexityScope(
        ActivityEnricher enricher,
        RequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichAnalyzeOperationComplexity(Context, Activity);
}
