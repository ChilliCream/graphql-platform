using System.Diagnostics;
using HotChocolate.Execution;

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
}
