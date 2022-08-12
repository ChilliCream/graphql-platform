using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Utilities.JsonValueToGraphQLValueConverter;
using ObjectType = HotChocolate.Fusion.Metadata.ObjectType;

namespace HotChocolate.Fusion.Execution;

internal sealed class RemoteQueryExecutor
{
    private readonly Metadata.ServiceConfiguration _serviceConfiguration;
    private readonly RemoteRequestExecutorFactory _executorFactory;

    public RemoteQueryExecutor(Metadata.ServiceConfiguration serviceConfiguration, RemoteRequestExecutorFactory executorFactory)
    {
        _serviceConfiguration = serviceConfiguration;
        _executorFactory = executorFactory;
    }

    public async Task<IQueryResult> ExecuteAsync(
        RemoteExecutorContext context,
        CancellationToken cancellationToken = default)
    {
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);
        var rootWorkItem = new WorkItem(rootSelectionSet, new ArgumentContext(), rootResult);
        context.Fetch.Add(rootWorkItem);

        while (context.Fetch.Count > 0)
        {
            await FetchAsync(context, cancellationToken).ConfigureAwait(false);

            while (context.Compose.TryDequeue(out var current))
            {
                ComposeResult(context, current);
            }
        }

        return QueryResultBuilder.New().SetData(rootResult).Create();
    }

    // note: this is inefficient and we want to group here, for now we just want to get it working.
    private async Task FetchAsync(RemoteExecutorContext context, CancellationToken ct)
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
                var result = await executor.ExecuteAsync(request, ct).ConfigureAwait(false);
                var data = requestNode.Handler.UnwrapResult(result);

                ExtractSelectionResults(selections, request.SchemaName, data, selectionResults);
                ExtractVariables(data, exportKeys, variableValues);
            }

            context.Compose.Enqueue(workItem);
        }

        context.Fetch.Clear();
    }

    private void ComposeResult(
        RemoteExecutorContext context,
        WorkItem workItem)
        => ComposeResult(
            context,
            workItem.SelectionSet.Selections,
            workItem.SelectionResults,
            workItem.Result,
            workItem.Variables);

    private void ComposeResult(
        RemoteExecutorContext context,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<SelectionResult> selectionResults,
        ObjectResult selectionSetResult,
        ArgumentContext variables)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];
            var selectionResult = selectionResults[i];
            var nullable = selection.TypeKind is not TypeKind.NonNull;
            var namedType = selection.Type.NamedType();

            if (selection.Type.IsScalarType() || namedType.IsScalarType())
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    selectionResult.Single.Element,
                    nullable);
            }
            else if (selection.Type.IsEnumType() || namedType.IsEnumType())
            {
                // we might need to map the enum value!
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    selectionResult.Single.Element,
                    nullable);
            }
            else if (selection.Type.IsCompositeType())
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    ComposeObject(context, selection, selectionResult, variables));
            }
            else
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    ComposeList(context, selection, selectionResult, variables, selection.Type));
            }
        }
    }

    private ListResult? ComposeList(
        RemoteExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ArgumentContext variables,
        Types.IType type)
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
                        new SelectionResult(new JsonResult(schemaName, item)),
                        variables));
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
                        variables,
                        elementType));
            }
        }

        return result;
    }

    private ObjectResult? ComposeObject(
        RemoteExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ArgumentContext variables)
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
                new WorkItem(fetchArguments, selectionSet, variables, result)
                {
                    SelectionResults = { [0] = selectionResult }
                });
        }
        else
        {
            var selections = selectionSet.Selections;
            var childSelectionResults = new SelectionResult[selections.Count];
            ExtractSelectionResults(selectionResult, selections, childSelectionResults);

            /*
            var childVariables = CreateVariables(
                context,
                selection,
                selectionResult,
                typeMetadata,
                variables);
                */

            ComposeResult(
                context,
                selectionSet.Selections,
                childSelectionResults,
                result,
                variables);
        }

        return result;
    }

    private IReadOnlyList<Argument> CreateArguments(
        RemoteExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata)
    {
        return new List<Argument>();
    }

    private ArgumentContext CreateVariables(
        RemoteExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata,
        ArgumentContext variables)
        => throw new NotImplementedException();

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
