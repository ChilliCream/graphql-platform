using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Utilities.JsonValueToGraphQLValueConverter;
using IType = HotChocolate.Types.IType;
using ObjectType = HotChocolate.Fusion.Metadata.ObjectType;

namespace HotChocolate.Fusion.Execution;

internal sealed class FederatedQueryExecutor
{
    private readonly ServiceConfiguration _serviceConfiguration;
    private readonly GraphQLClientFactory _executorFactory;

    public FederatedQueryExecutor(
        ServiceConfiguration serviceConfiguration,
        GraphQLClientFactory executorFactory)
    {
        _serviceConfiguration = serviceConfiguration ??
            throw new ArgumentNullException(nameof(serviceConfiguration));
        _executorFactory = executorFactory ??
            throw new ArgumentNullException(nameof(executorFactory));
    }

    public async Task<IQueryResult> ExecuteAsync(
        FederatedQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var scopedContext = ImmutableDictionary<string, object?>.Empty;
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);
        var rootWorkItem = new WorkItem(rootSelectionSet, rootResult);
        context.Fetch.Add(rootWorkItem);

        // introspection ... needs to be integrated into the execution plan.
        if (context.Plan.HasIntrospectionSelections)
        {
            var rootSelections = rootSelectionSet.Selections;
            var operationContext = context.OperationContext;

            for (var i = 0; i < rootSelections.Count; i++)
            {
                var selection = rootSelections[i];
                if (selection.Field.IsIntrospectionField)
                {
                    var resolverTask = operationContext.CreateResolverTask(
                        selection,
                        operationContext.RootValue,
                        rootResult,
                        i,
                        operationContext.PathFactory.Append(Path.Root, selection.ResponseName),
                        scopedContext);
                    resolverTask.BeginExecute(cancellationToken);

                    // todo : this is just temporary
                    await resolverTask.WaitForCompletionAsync(cancellationToken);
                }
            }
        }

        // execute execution plan
        while (context.Fetch.Count > 0)
        {
            await FetchAsync(context, cancellationToken).ConfigureAwait(false);

            while (context.Compose.TryDequeue(out var current))
            {
                ComposeResult(context, current);
            }
        }

        context.Result.SetData(rootResult);
        return context.Result.BuildResult();
    }

    // note: this is inefficient and we want to group here, for now we just want to get it working.
    private async Task FetchAsync(FederatedQueryContext context, CancellationToken ct)
    {
        foreach (var workItem in context.Fetch)
        {
            // todo: this is not really efficient
            var variableValues = new Dictionary<string, IValueNode>(StringComparer.Ordinal);
            var selectionResults = workItem.SelectionResults;
            var partialResult = selectionResults[0];
            var selections = workItem.SelectionSet.Selections;
            var exportKeys = context.Plan.GetExports(workItem.SelectionSet);

            // if there was a partial result stored on the selection set then we will first unwrap
            // it before starting to fetch.
            if (partialResult.HasValue)
            {
                // first we need to erase the partial result from the array so that its not
                // combined into the result creation.
                selectionResults[0] = default;

                // next we will unwrap the results.
                ExtractSelectionResults(partialResult, selections, selectionResults);

                // last we will check if there are any exports for this selection-set.
                ExtractVariables(partialResult, exportKeys, variableValues);
            }

            foreach (var requestNode in context.Plan.GetRequestNodes(workItem.SelectionSet))
            {
                var executor = _executorFactory.Create(requestNode.Handler.SchemaName);
                var request = requestNode.Handler.CreateRequest(variableValues);
                var response = await executor.ExecuteAsync(request, ct).ConfigureAwait(false);
                var data = requestNode.Handler.UnwrapResult(response);

                ExtractSelectionResults(selections, request.SchemaName, data, selectionResults);
                ExtractVariables(data, exportKeys, variableValues);

                context.Result.RegisterForCleanup(() =>
                {
                    response.Dispose();
                    return default;
                });
            }

            context.Compose.Enqueue(workItem);
        }

        context.Fetch.Clear();
    }

    private void ComposeResult(
        FederatedQueryContext context,
        WorkItem workItem)
        => ComposeResult(
            context,
            workItem.SelectionSet.Selections,
            workItem.SelectionResults,
            workItem.Result);

    private void ComposeResult(
        FederatedQueryContext context,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<SelectionResult> selectionResults,
        ObjectResult selectionSetResult)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];
            var selectionType = selection.Type;
            var responseName = selection.ResponseName;
            var field = selection.Field;

            if (!field.IsIntrospectionField)
            {
                var selectionResult = selectionResults[i];
                var nullable = selection.TypeKind is not TypeKind.NonNull;
                var namedType = selectionType.NamedType();

                if (namedType.IsScalarType())
                {
                    var value = selectionResult.Single.Element;
                    selectionSetResult.SetValueUnsafe(i, responseName, value, nullable);
                }
                else if (namedType.IsEnumType())
                {
                    // we might need to map the enum value!
                    var value = selectionResult.Single.Element;
                    selectionSetResult.SetValueUnsafe(i, responseName, value, nullable);
                }
                else if (selectionType.IsCompositeType())
                {
                    var value = ComposeObject(context, selection, selectionResult);
                    selectionSetResult.SetValueUnsafe(i, responseName, value);
                }
                else
                {
                    var value = ComposeList(context, selection, selectionResult, selectionType);
                    selectionSetResult.SetValueUnsafe(i, responseName, value);
                }
            }
            else if (field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
            {
                var value = selection.DeclaringType.Name;
                selectionSetResult.SetValueUnsafe(i, responseName, value, false);
            }
        }
    }

    private ListResult? ComposeList(
        FederatedQueryContext context,
        ISelection selection,
        SelectionResult selectionResult,
        IType type)
    {
        if (selectionResult.IsNull())
        {
            return null;
        }

        var json = selectionResult.Single.Element;
        var schemaName = selectionResult.Single.SchemaName;
        Debug.Assert(selectionResult.Multiple is null, "selectionResult.Multiple is null");
        Debug.Assert(json.ValueKind is JsonValueKind.Array, "json.ValueKind is JsonValueKind.Array");

        var elementType = type.ElementType();
        var result = context.Result.RentList(json.GetArrayLength());

        if (!elementType.IsListType())
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeObject(
                        context,
                        selection,
                        new SelectionResult(new JsonResult(schemaName, item))));
            }
        }
        else
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeList(
                        context,
                        selection,
                        new SelectionResult(new JsonResult(schemaName, item)),
                        elementType));
            }
        }

        return result;
    }

    private ObjectResult? ComposeObject(
        FederatedQueryContext context,
        ISelection selection,
        SelectionResult selectionResult)
    {
        if (selectionResult.IsNull())
        {
            return null;
        }

        ObjectType typeMetadata;
        Types.ObjectType type;

        if (selection.Type.NamedType() is Types.ObjectType ot)
        {
            type = ot;
            typeMetadata = _serviceConfiguration.GetType<ObjectType>(ot.Name);
        }
        else
        {
            var typeInfo = selectionResult.GetTypeInfo();
            typeMetadata = _serviceConfiguration.GetType<ObjectType>(typeInfo);
            type = context.Schema.GetType<Types.ObjectType>(typeMetadata.Name);
        }

        var selectionSet = context.Operation.GetSelectionSet(selection, type);
        var result = context.Result.RentObject(selectionSet.Selections.Count);

        if (context.RequiresFetch.Contains(selectionSet))
        {
            var fetchArguments = CreateArguments(
                context,
                selection,
                selectionResult,
                typeMetadata);

            context.Fetch.Add(
                new WorkItem(fetchArguments, selectionSet, result)
                {
                    SelectionResults = { [0] = selectionResult }
                });
        }
        else
        {
            var selections = selectionSet.Selections;
            var childSelectionResults = new SelectionResult[selections.Count];
            ExtractSelectionResults(selectionResult, selections, childSelectionResults);
            ComposeResult(context, selectionSet.Selections, childSelectionResults, result);
        }

        return result;
    }

    private IReadOnlyList<Argument> CreateArguments(
        FederatedQueryContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata)
        => Array.Empty<Argument>();

    private static void ExtractSelectionResults(
        SelectionResult parent,
        IReadOnlyList<ISelection> selections,
        SelectionResult[] selectionResults)
    {
        if (parent.Multiple is null)
        {
            var schemaName = parent.Single.SchemaName;
            var data = parent.Single.Element;

            for (var i = 0; i < selections.Count; i++)
            {
                if (data.TryGetProperty(selections[i].ResponseName, out var property))
                {
                    var current = selectionResults[i];

                    selectionResults[i] = current.HasValue
                        ? current.AddResult(new JsonResult(schemaName, property))
                        : new SelectionResult(new JsonResult(schemaName, property));
                }
            }
        }
        else
        {
            foreach (var result in parent.Multiple)
            {
                var schemaName = result.SchemaName;
                var data = result.Element;

                for (var i = 0; i < selections.Count; i++)
                {
                    if (data.TryGetProperty(selections[i].ResponseName, out var property))
                    {
                        var current = selectionResults[i];

                        selectionResults[i] = current.HasValue
                            ? current.AddResult(new JsonResult(schemaName, property))
                            : new SelectionResult(new JsonResult(schemaName, property));
                    }
                }
            }
        }
    }

    private static void ExtractSelectionResults(
        IReadOnlyList<ISelection> selections,
        string schemaName,
        JsonElement data,
        SelectionResult[] selectionResults)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            if (data.TryGetProperty(selections[i].ResponseName, out var property))
            {
                var selectionResult = selectionResults[i];
                if (selectionResult.HasValue)
                {
                    selectionResults[i] = selectionResult.AddResult(new(schemaName, property));
                }
                else
                {
                    selectionResults[i] = new SelectionResult(new JsonResult(schemaName, property));
                }
            }
        }
    }

    private static void ExtractVariables(
        SelectionResult parent,
        IReadOnlyList<string> exportKeys,
        Dictionary<string, IValueNode> variableValues)
    {
        if (exportKeys.Count > 0)
        {
            if (parent.Multiple is null)
            {
                ExtractVariables(parent.Single.Element, exportKeys, variableValues);
            }
            else
            {
                foreach (var result in parent.Multiple)
                {
                    ExtractVariables(result.Element, exportKeys, variableValues);
                }
            }
        }
    }

    private static void ExtractVariables(
        JsonElement parent,
        IReadOnlyList<string> exportKeys,
        Dictionary<string, IValueNode> variableValues)
    {
        if (exportKeys.Count > 0 &&
            parent.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            for (var i = 0; i < exportKeys.Count; i++)
            {
                var key = exportKeys[i];

                if (!variableValues.ContainsKey(key) &&
                    parent.TryGetProperty(key, out var property))
                {
                    variableValues.TryAdd(key, Convert(property));
                }
            }
        }
    }
}
