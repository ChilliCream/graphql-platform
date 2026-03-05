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
        if (context.TryGetOperation(out var operation))
        {
            Activity.SetStatus(ActivityStatusCode.Ok);

            Activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operation.Kind]);

            var operationName = operation.Name;
            if (!string.IsNullOrEmpty(operationName))
            {
                Activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        enricher.EnrichCompileOperation(Activity, context);
    }
}
