using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class ValidateDocumentScope : RequestScopeBase
{
    public ValidateDocumentScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichValidateDocument(Context, Activity);
}
