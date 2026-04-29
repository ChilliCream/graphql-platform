using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class CoerceVariablesScope : RequestScopeBase
{
    public CoerceVariablesScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichCoerceVariables(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.Variables is not null)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
