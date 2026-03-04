using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ValidationSpan(Activity activity, RequestContext context) : SpanBase(activity)
{
    public static ParsingSpan? Start(ActivitySource source, RequestContext context)
    {
        var activity = source.StartActivity( "GraphQL Document Validation");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        if (context.TryGetDocument(out var document, out _)
            && document.GetOperation(context.Request.OperationName) is { } operation)
        {
            activity.SetTag(GraphQL.Operation.Type, operation.Operation);
        }

        var operationName = context.Request.OperationName;
        // TODO: This should be conditional
        if (!string.IsNullOrEmpty(operationName))
        {
            activity.SetTag(GraphQL.Operation.Name, operationName);
        }

        var documentInfo = context.OperationDocumentInfo;
        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        // TODO: We need a good mechanism to determine if persisted operations are enabled
        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new ParsingSpan(activity, context);
    }

    protected override void OnComplete()
    {
        if (context.IsOperationDocumentValid())
        {
            Activity.MarkAsSuccess();
        }
    }
}
