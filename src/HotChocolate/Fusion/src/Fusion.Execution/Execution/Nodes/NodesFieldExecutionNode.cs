using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Dispatches a plural Global Object Identification field to one variable-batched branch per
/// concrete object type.
/// </summary>
public sealed class NodesFieldExecutionNode : ExecutionNode
{
    internal const string IdVariableName = "__fusion_nodes_id";

    private readonly Dictionary<string, ExecutionNode> _branches = [];
    private readonly string _responseName;
    private readonly IValueNode _idsValue;
    private readonly ExecutionNodeCondition[] _conditions;

    internal NodesFieldExecutionNode(
        int id,
        string responseName,
        IValueNode idsValue,
        ExecutionNodeCondition[] conditions)
    {
        Id = id;
        _responseName = responseName;
        _idsValue = idsValue;
        _conditions = conditions;
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.Nodes;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    public override string? SchemaName => null;

    public Dictionary<string, ExecutionNode> Branches => _branches;

    public string ResponseName => _responseName;

    public IValueNode IdsValue => _idsValue;

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var ids = GetIds(context);
        var groups = new Dictionary<string, List<NodeIdValue>>(StringComparer.Ordinal);

        context.InitializeNodesResult(_responseName, ids.Count);
        BeginDependentSelection(context);

        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];

            if (!context.TryParseTypeNameFromId(id, out var typeName)
                || !_branches.ContainsKey(typeName))
            {
                context.AddNodesError(
                    _responseName,
                    i,
                    ErrorHelper.InvalidNodeIdFormat(id));
                continue;
            }

            if (!groups.TryGetValue(typeName, out var group))
            {
                group = [];
                groups.Add(typeName, group);
            }

            group.Add(new NodeIdValue(i, id));
        }

        foreach (var (typeName, values) in groups)
        {
            var branch = _branches[typeName];
            var variables = context.CreateNodesVariableValueSets(
                _responseName,
                IdVariableName,
                values);
            context.SetDynamicVariableValueSets(branch, variables);
            EnqueueDependentForExecution(context, branch);
        }

        return ValueTask.FromResult(ExecutionStatus.Success);
    }

    internal void AddBranch(string objectTypeName, ExecutionNode node)
    {
        ArgumentException.ThrowIfNullOrEmpty(objectTypeName);
        ArgumentNullException.ThrowIfNull(node);
        ExpectMutable();
        _branches[objectTypeName] = node;
    }

    private IReadOnlyList<string> GetIds(OperationPlanContext context)
    {
        IValueNode? value = _idsValue;

        if (value is VariableNode variable)
        {
            if (!context.Variables.TryGetValue(variable.Name.Value, out value) || value is null)
            {
                throw new InvalidOperationException(
                    $"Expected to find a value for variable '{variable.Name.Value}'.");
            }
        }

        if (value is ListValueNode list)
        {
            var values = new string[list.Items.Count];
            for (var i = 0; i < list.Items.Count; i++)
            {
                values[i] = GetIdValue(list.Items[i]);
            }

            return values;
        }

        return [GetIdValue(value)];

        static string GetIdValue(IValueNode idValue)
            => idValue switch
            {
                StringValueNode stringValue => stringValue.Value,
                IntValueNode intValue => intValue.Value,
                _ => throw new InvalidOperationException("Expected an ID value.")
            };
    }
}

internal readonly record struct NodeIdValue(int Index, string Id);
