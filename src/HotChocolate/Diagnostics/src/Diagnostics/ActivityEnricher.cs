using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

/// <summary>
/// The activity enricher is used to add information to the activity spans.
/// You can inherit from this class and override the enricher methods to provide more or
/// less information.
/// </summary>
public class ActivityEnricher(
    ObjectPool<StringBuilder> stringBuilderPool,
    InstrumentationOptions options) : ActivityEnricherBase(stringBuilderPool, options)
{
    public virtual void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDisplayName = CreateOperationDisplayName(context, operation);

        EnrichExecuteRequestCore(
            context,
            activity,
            operationDisplayName,
            operation?.Id,
            operation?.Kind,
            operation?.Name);
    }

    public virtual void EnrichParseDocument(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDefinition = ResolveOperationDefinition(
            operation?.Definition,
            context.OperationDocumentInfo,
            context.Request.OperationName);

        EnrichParseDocumentCore(activity, operationDefinition, context.OperationDocumentInfo);
    }

    public virtual void EnrichValidateDocument(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDefinition = ResolveOperationDefinition(
            operation?.Definition,
            context.OperationDocumentInfo,
            context.Request.OperationName);

        EnrichValidateDocumentCore(activity, operationDefinition, context.OperationDocumentInfo);
    }

    public virtual void EnrichCoerceVariables(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDefinition = ResolveOperationDefinition(
            operation?.Definition,
            context.OperationDocumentInfo,
            context.Request.OperationName);

        EnrichCoerceVariablesCore(activity, operationDefinition, context.OperationDocumentInfo);
    }

    public virtual void EnrichCompileOperation(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDefinition = ResolveOperationDefinition(
            operation?.Definition,
            context.OperationDocumentInfo,
            context.Request.OperationName);

        activity.DisplayName = "GraphQL Operation Planning";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        EnrichWithTags(activity, operationDefinition, context.OperationDocumentInfo);
    }

    public virtual void EnrichExecuteOperation(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var operationDefinition = ResolveOperationDefinition(
            operation?.Definition,
            context.OperationDocumentInfo,
            context.Request.OperationName);

        activity.DisplayName = "GraphQL Operation Execution";

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        EnrichWithTags(activity, operationDefinition, context.OperationDocumentInfo);
    }

    public virtual void EnrichResolveFieldValue(IMiddlewareContext context, Activity activity)
    {
        var selection = context.Selection;
        var coordinate = selection.Field.Coordinate;
        var path = FormatPath(context.Path);

        activity.DisplayName = coordinate.ToString();
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Resolve);
        activity.SetTag(GraphQL.Selection.Name, selection.ResponseName);
        activity.SetTag(GraphQL.Selection.Path, path);
        activity.SetTag(GraphQL.Selection.Field.Name, coordinate.MemberName);
        activity.SetTag(GraphQL.Selection.Field.Coordinate, activity.DisplayName);
        activity.SetTag(GraphQL.Selection.Field.ParentType, coordinate.Name);
    }

    public virtual void EnrichResolverError(
        RequestContext context,
        IError error,
        Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichResolverError(
        IMiddlewareContext middlewareContext,
        IError error,
        Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichDataLoaderBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys,
        Activity activity)
        where TKey : notnull
    {
        activity.DisplayName = $"Execute {dataLoader.GetType().Name} Batch";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.DataLoaderBatch);
        activity.SetTag(GraphQL.DataLoader.Batch.Size, keys.Count);

        if (options.IncludeDataLoaderKeys)
        {
            var temp = keys.Select(t => t.ToString()).ToArray();
            activity.SetTag(GraphQL.DataLoader.Batch.Keys, temp);
        }
    }

    public virtual void EnrichDataLoaderBatchDispatchCoordinator(Activity activity)
    {
        activity.DisplayName = "Coordinate DataLoader Batches";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.DataLoaderDispatch);
    }

    protected virtual string? CreateOperationDisplayName(RequestContext context, Operation? operation)
    {
        return operation is null
            ? null
            : BuildOperationDisplayName(operation.Definition.Operation, operation.Name);
    }
}
