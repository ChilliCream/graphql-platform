using System.Diagnostics;
using HotChocolate.Diagnostics.Scopes;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics;

internal sealed class ParseDocumentScope : RequestScopeBase
{
    public ParseDocumentScope(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichParseDocument(Context, Activity);
}
