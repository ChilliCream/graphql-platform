using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.ExecutorUtils;
using GraphQLRequest = HotChocolate.Fusion.Clients.GraphQLRequest;

namespace HotChocolate.Fusion.Planning;

internal sealed class BatchByKeyResolverNode : QueryPlanNode
{
    private readonly IReadOnlyList<string> _path;

    public BatchByKeyResolverNode(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyDictionary<string, ITypeNode> argumentTypes)
        : base(id)
    {
        SubgraphName = subgraphName;
        Document = document;
        SelectionSet = selectionSet;
        Requires = requires;
        ArgumentTypes = argumentTypes;
        _path = path;
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.BatchResolver;

    /// <summary>
    /// Gets the schema name on which this request handler executes.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the selection set for which this request provides a patch.
    /// </summary>
    public ISelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the variables that this request handler requires to create a request.
    /// </summary>
    public IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// Gets the type lookup of resolver arguments.
    /// </summary>
    /// <value></value>
    public IReadOnlyDictionary<string, ITypeNode> ArgumentTypes { get; }

    protected override async Task OnExecuteAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(SelectionSet, out var originalWorkItems))
        {
            for (var i = 0; i < originalWorkItems.Count; i++)
            {
                ExtractPartialResult(originalWorkItems[i]);
            }

            var workItems = CreateBatchWorkItem(originalWorkItems);
            var subgraphName = SubgraphName;
            var firstWorkItem = workItems[0];
            var selections = firstWorkItem.SelectionSet.Selections;

            // Create the batch subgraph request.
            var variableValues = BuildVariables(workItems);
            var request = CreateRequest(variableValues);

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

            var result = UnwrapResult(response, firstWorkItem.ExportKeys);

            for (var i = 0; i < workItems.Length; i++)
            {
                var workItem = workItems[i];
                if (result.TryGetValue(workItem.Key, out var workItemData))
                {
                    ExtractSelectionResults(selections, subgraphName, workItemData, workItem.SelectionResults);
                    ExtractVariables(workItemData, firstWorkItem.ExportKeys, workItem.VariableValues);
                }
            }

        }
    }

    protected override async Task OnExecuteNodesAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.ContainsState(SelectionSet))
        {
            await base.OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
    }

    private GraphQLRequest CreateRequest(IReadOnlyDictionary<string, IValueNode> variableValues)
    {
        ObjectValueNode? vars = null;

        if (Requires.Count > 0)
        {
            var fields = new List<ObjectFieldNode>();

            foreach (var requirement in Requires)
            {
                if (variableValues.TryGetValue(requirement, out var value))
                {
                    fields.Add(new ObjectFieldNode(requirement, value));
                }
                else
                {
                    // TODO : error helper
                    throw new ArgumentException(
                        $"The variable value `{requirement}` was not provided " +
                        "but is required.",
                        nameof(variableValues));
                }
            }

            vars ??= new ObjectValueNode(fields);
        }

        return new GraphQLRequest(SubgraphName, Document, vars, null);
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

        if (_path.Count > 0)
        {
            data = LiftData();
        }

        var result = new Dictionary<string, JsonElement>();

        if (exportKeys.Count == 1)
        {
            var key = exportKeys[0];
            foreach (var element in data.EnumerateArray())
            {
                if (element.TryGetProperty(key, out var keyValue))
                {
                    result.TryAdd(keyValue.ToString(), element);
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
                        key += keyValue.ToString();
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

    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteString("schemaName", SubgraphName);
        writer.WriteString("document", Document.ToString(false));
        writer.WriteNumber("selectionSetId", SelectionSet.Id);

        if (_path.Count > 0)
        {
            writer.WritePropertyName("path");
            writer.WriteStartArray();

            foreach (var path in _path)
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
    }

    private static BatchWorkItem[] CreateBatchWorkItem(IReadOnlyList<WorkItem> workItems)
    {
        var exportKeys = workItems[0].ExportKeys;
        var batchWorkItems = new BatchWorkItem[workItems.Count];

        if (exportKeys.Count == 1)
        {
            for (var i = 0; i < workItems.Count; i++)
            {
                var workItem = workItems[i];
                var key = workItem.VariableValues.First().Value.ToString();
                batchWorkItems[i] = new BatchWorkItem(key, workItem);
            }
        }
        else
        {
            for (var i = 0; i < workItems.Count; i++)
            {
                var workItem = workItems[i];
                var key = string.Empty;

                for (var j = 0; j < exportKeys.Count; j++)
                {
                    var value = workItem.VariableValues[exportKeys[j]].ToString();
                    key += value;
                }

                batchWorkItems[i] = new BatchWorkItem(key, workItem);
            }
        }

        return batchWorkItems;
    }

    private readonly struct BatchWorkItem
    {
        public BatchWorkItem(
            string batchKey,
            WorkItem workItem)
        {
            Key = batchKey;
            VariableValues = workItem.VariableValues;
            ExportKeys = workItem.ExportKeys;
            SelectionSet = workItem.SelectionSet;
            SelectionResults = workItem.SelectionResults;
            Result = workItem.Result;
        }

        public string Key { get; }

        public Dictionary<string, IValueNode> VariableValues { get; }

        public IReadOnlyList<string> ExportKeys { get; }

        public ISelectionSet SelectionSet { get; }

        public SelectionResult[] SelectionResults { get; }

        public ObjectResult Result { get; }
    }
}
