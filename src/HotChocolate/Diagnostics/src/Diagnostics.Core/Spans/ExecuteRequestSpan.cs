using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

// TODO: Needs additional tags probably
internal sealed class ExecuteRequestSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase? enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public static ExecuteRequestSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher,
        InstrumentationOptionsBase options)
    {
        var activity = source.StartActivity("GraphQL Operation", ActivityKind.Server);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        var documentInfo = context.OperationDocumentInfo;

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        return new ExecuteRequestSpan(activity, context, enricher, true);
    }

    protected override void OnComplete()
    {
        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.MarkAsError();
        }
        else
        {
            Activity.MarkAsSuccess();
        }

        enricher?.EnrichExecuteRequest(Activity, context);
    }
}
