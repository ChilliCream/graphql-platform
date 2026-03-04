using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
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

        EnrichParseDocumentCore(activity, operation?.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichValidateDocument(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);

        EnrichValidateDocumentCore(activity, operation?.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichCoerceVariables(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);

        EnrichCoerceVariablesCore(activity, operation?.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichCompileOperation(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);

        activity.DisplayName = "Compile Operation";
        // TODO: Not sure if plan is supposed to be used like this
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        EnrichWithTags(activity, operation?.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichExecuteOperation(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        activity.DisplayName =
            operation?.Name is { } op
                ? $"Execute Operation {op}"
                : "Execute Operation";

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        EnrichWithTags(activity, operation?.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichResolveFieldValue(IMiddlewareContext context, Activity activity)
    {
        string path;
        string hierarchy;
        BuildPath();

        var selection = context.Selection;
        var coordinate = selection.Field.Coordinate;

        activity.DisplayName = path;
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Resolve);
        activity.SetTag(GraphQL.Selection.Name, selection.ResponseName);
        activity.SetTag(GraphQL.Selection.Field.Type, selection.Field.Type.Print());
        activity.SetTag(GraphQL.Selection.Path, path);
        activity.SetTag(GraphQL.Selection.Hierarchy, hierarchy);
        activity.SetTag(GraphQL.Selection.Field.Name, coordinate.MemberName);
        activity.SetTag(GraphQL.Selection.Field.Coordinate, coordinate.ToString());
        activity.SetTag(GraphQL.Selection.Field.ParentType, coordinate.Name);
        activity.SetTag(GraphQL.Selection.Field.IsDeprecated, selection.Field.IsDeprecated);

        void BuildPath()
        {
            var p = StringBuilderPool.Get();
            var h = StringBuilderPool.Get();
            var index = StringBuilderPool.Get();

            var current = context.Path;

            do
            {
                if (current is NamePathSegment n)
                {
                    p.Insert(0, '/');
                    h.Insert(0, '/');
                    p.Insert(1, n.Name);
                    h.Insert(1, n.Name);

                    if (index.Length > 0)
                    {
                        p.Insert(1 + n.Name.Length, index);
                    }

                    index.Clear();
                }

                if (current is IndexerPathSegment i)
                {
                    var number = i.Index.ToString();
                    index.Insert(0, '[');
                    index.Insert(1, number);
                    index.Insert(1 + number.Length, ']');
                }

                current = current.Parent;
            } while (!current.IsRoot);

            path = p.ToString();
            hierarchy = h.ToString();

            StringBuilderPool.Return(p);
            StringBuilderPool.Return(h);
            StringBuilderPool.Return(index);
        }
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
        if (operation is null)
        {
            return null;
        }

        var selections = operation.RootSelectionSet.Selections;
        var names = new string[selections.Length];

        for (var i = 0; i < selections.Length; i++)
        {
            names[i] = selections[i].ResponseName;
        }

        return BuildOperationDisplayName(
            operation.Definition.Operation,
            operation.Name,
            names.Length,
            names);
    }
}
