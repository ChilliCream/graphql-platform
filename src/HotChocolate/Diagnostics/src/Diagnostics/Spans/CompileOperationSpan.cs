using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class CompileOperationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricher enricher) : SpanBase(activity)
{
    public static CompileOperationSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricher enricher)
    {
        var activity = source.StartActivity("GraphQL Operation Planning");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        if (context.TryGetDocument(out var document, out _)
            && document.GetOperation(context.Request.OperationName) is { } operation)
        {
            activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operation.Operation]);

            var operationName = operation.Name?.Value;
            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo is { IsPersisted: true, Id.HasValue: true })
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new CompileOperationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.TryGetOperation(out _))
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichCompileOperation(Activity, context);
    }
}
