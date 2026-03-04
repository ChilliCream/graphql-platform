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
    bool shouldDisposeActivity) : IDisposable
{
    private bool _disposed;

    public Activity Activity { get; } = activity;

    public static ExecuteRequestSpan? Start(
        ActivitySource source,
        RequestContext context,
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

        return new ExecuteRequestSpan(activity, context, true);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            OnComplete();

            if (shouldDisposeActivity)
            {
                Activity.Dispose();
            }
        }
    }

    private void OnComplete()
    {
        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.MarkAsError();
        }
        else
        {
            Activity.MarkAsSuccess();
        }
    }
}
