using System.Diagnostics;
using HotChocolate.Execution;

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
}
