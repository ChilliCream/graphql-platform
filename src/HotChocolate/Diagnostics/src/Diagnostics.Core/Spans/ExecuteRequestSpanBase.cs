using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal abstract class ExecuteRequestSpanBase(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase? enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public RequestContext Context { get; } = context;

    protected static Activity? StartActivity(ActivitySource source)
    {
        return source.StartActivity("GraphQL Operation", ActivityKind.Server);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        var documentInfo = Context.OperationDocumentInfo;

        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            Activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo is { IsPersisted: true, Id.HasValue: true })
        {
            Activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            Activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (Context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);
        }
        else
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher?.EnrichExecuteRequest(Activity, Context);
    }
}
