using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.ExecutorUtils;

namespace HotChocolate.Fusion.Planning;

internal sealed class ResolveByKeyBatch : ResolverNodeBase
{
    private readonly IReadOnlyList<string> _path;

    public ResolveByKeyBatch(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyDictionary<string, ITypeNode> argumentTypes,
        IReadOnlyList<string> forwardedVariables,
        TransportFeatures transportFeatures)
        : base(id, subgraphName, document, selectionSet, requires, path, forwardedVariables, transportFeatures)
    {
        ArgumentTypes = argumentTypes;
        _path = path;
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.ResolveByKeyBatch;

    /// <summary>
    /// Gets the type lookup of resolver arguments.
    /// </summary>
    public IReadOnlyDictionary<string, ITypeNode> ArgumentTypes { get; }

    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(SelectionSet, out var originalWorkItems))
        {
            for (var i = 0; i < originalWorkItems.Count; i++)
            {
                TryInitializeWorkItem(context.QueryPlan, originalWorkItems[i]);
            }

            var workItems = CreateBatchWorkItem(originalWorkItems, Requires);
            var subgraphName = SubgraphName;
            var firstWorkItem = workItems[0];

            // Create the batch subgraph request.
            var variableValues = BuildVariables(workItems);
            var request = CreateRequest(
                context.OperationContext.Variables,
                variableValues);

            // Once we have the batch request, we will enqueue it for execution with
            // the execution engine.
            var response = await context.ExecuteAsync(
                    subgraphName,
                    request,
                    cancellationToken)
                .ConfigureAwait(false);

            // Before we extract the data from the responses we will enqueue the responses
            // for cleanup so that the memory can be released at the end of the execution.
            context.Result.RegisterForCleanup(
                response,
                r =>
                {
                    r.Dispose();
                    return default!;
                });

            ExtractErrors(context.Result, response.Errors, context.ShowDebugInfo);
            var result = UnwrapResult(response, Requires);

            for (var i = 0; i < workItems.Length; i++)
            {
                var workItem = workItems[i];
                if (result.TryGetValue(workItem.Key, out var workItemData))
                {
                    ExtractSelectionResults(SelectionSet, subgraphName, workItemData, workItem.SelectionResults);
                    ExtractVariables(workItemData, context.QueryPlan, SelectionSet, firstWorkItem.ExportKeys, workItem.VariableValues);
                }
            }
        }
    }

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.ContainsState(SelectionSet))
        {
            await base.OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
    }

    private Dictionary<string, IValueNode> BuildVariables(BatchWorkItem[] workItems)
    {
        if (workItems.Length == 1)
        {
            return workItems[0].VariableValues;
        }

        var variableValues = new Dictionary<string, IValueNode>();
        var uniqueWorkItems = new List<BatchWorkItem>();
        var processed = new HashSet<string>();

        foreach (var workItem in workItems)
        {
            if (processed.Add(workItem.Key))
            {
                uniqueWorkItems.Add(workItem);
            }
        }

        foreach (var key in workItems[0].VariableValues.Keys)
        {
            var expectedType = ArgumentTypes[key];

            if (expectedType.IsListType())
            {
                var list = new List<IValueNode>();

                foreach (var value in uniqueWorkItems)
                {
                    if (value.VariableValues.TryGetValue(key, out var variableValue))
                    {
                        list.Add(variableValue);
                    }
                }

                variableValues.Add(key, new ListValueNode(list));
            }
            else
            {
                if (workItems[0].VariableValues.TryGetValue(key, out var variableValue))
                {
                    variableValues.Add(key, variableValue);
                }
            }
        }

        return variableValues;
    }

    private Dictionary<string, JsonElement> UnwrapResult(
        GraphQLResponse response,
        IReadOnlyList<string> exportKeys)
    {
        var data = response.Data;

        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return new Dictionary<string, JsonElement>();
        }

        if (_path.Count > 0)
        {
            data = LiftData();
        }

        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return new Dictionary<string, JsonElement>();
        }

        var result = new Dictionary<string, JsonElement>();

        if (exportKeys.Count == 1)
        {
            var key = exportKeys[0];
            foreach (var element in data.EnumerateArray())
            {
                if (element.TryGetProperty(key, out var keyValue))
                {
                    result.TryAdd(FormatKeyValue(keyValue), element);
                }
            }
        }
        else
        {
            foreach (var element in data.EnumerateArray())
            {
                var key = string.Empty;

                foreach (var exportKey in exportKeys)
                {
                    if (element.TryGetProperty(exportKey, out var keyValue))
                    {
                        key += FormatKeyValue(keyValue);
                    }
                }

                result.TryAdd(key, element);
            }
        }

        return result;

        JsonElement LiftData()
        {
            if (data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                var current = data;

                for (var i = 0; i < _path.Count; i++)
                {
                    if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                    {
                        return current;
                    }

                    current.TryGetProperty(_path[i], out var propertyValue);
                    current = propertyValue;
                }

                return current;
            }

            return data;
        }
    }

    private static BatchWorkItem[] CreateBatchWorkItem(
        IReadOnlyList<SelectionSetState> workItems,
        IReadOnlyList<string> requirements)
    {
        var batchWorkItems = new BatchWorkItem[workItems.Count];

        if (requirements.Count == 1)
        {
            for (var i = 0; i < workItems.Count; i++)
            {
                var workItem = workItems[i];
                var key = FormatKeyValue(workItem.VariableValues[requirements[0]]);
                batchWorkItems[i] = new BatchWorkItem(key, workItem);
            }
        }
        else
        {
            for (var i = 0; i < workItems.Count; i++)
            {
                var workItem = workItems[i];
                var key = string.Empty;

                for (var j = 0; j < requirements.Count; j++)
                {
                    var requirement = requirements[j];
                    var value = FormatKeyValue(workItem.VariableValues[requirement]);
                    key += value;
                }

                batchWorkItems[i] = new BatchWorkItem(key, workItem);
            }
        }

        return batchWorkItems;
    }

    /// <summary>
    /// Formats the properties of this query plan node in order to create a JSON representation.
    /// </summary>
    /// <param name="writer">
    /// The writer that is used to write the JSON representation.
    /// </param>
    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteString("subgraph", SubgraphName);
        writer.WriteString("document", Document);
        writer.WriteNumber("selectionSetId", SelectionSet.Id);

        if (ArgumentTypes.Count > 0)
        {
            writer.WritePropertyName("argumentTypes");
            writer.WriteStartArray();

            foreach (var (argument, type) in ArgumentTypes)
            {
                writer.WriteStartObject();
                writer.WriteString("argument", argument);
                writer.WriteString("type", type.ToString(false));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (Path.Count > 0)
        {
            writer.WritePropertyName("path");
            writer.WriteStartArray();

            foreach (var path in Path)
            {
                writer.WriteStringValue(path);
            }
            writer.WriteEndArray();
        }

        if (Requires.Count > 0)
        {
            writer.WritePropertyName("requires");
            writer.WriteStartArray();

            foreach (var requirement in Requires)
            {
                writer.WriteStartObject();
                writer.WriteString("variable", requirement);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (ForwardedVariables.Count > 0)
        {
            writer.WritePropertyName("forwardedVariables");
            writer.WriteStartArray();

            foreach (var variable in ForwardedVariables)
            {
                writer.WriteStartObject();
                writer.WriteString("variable", variable);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatKeyValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => throw new NotSupportedException(),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatKeyValue(IValueNode element)
        => element switch
        {
            StringValueNode value => value.Value,
            IntValueNode value => value.ToString(),
            FloatValueNode value => value.ToString(),
            BooleanValueNode { Value: true } => "true",
            BooleanValueNode { Value: false } => "false",
            NullValueNode => "null",
            _ => throw new NotSupportedException(),
        };

    private readonly struct BatchWorkItem
    {
        public BatchWorkItem(
            string batchKey,
            SelectionSetState selectionSetState)
        {
            Key = batchKey;
            VariableValues = selectionSetState.VariableValues;
            ExportKeys = selectionSetState.ExportKeys;
            SelectionResults = selectionSetState.SelectionSetData;
        }

        public string Key { get; }

        public Dictionary<string, IValueNode> VariableValues { get; }

        public IReadOnlyList<string> ExportKeys { get; }

        public SelectionData[] SelectionResults { get; }
    }
}
