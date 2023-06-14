using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanContext
{
    private readonly Dictionary<ExecutionStep, QueryPlanNode> _stepToNode = new();
    private readonly Dictionary<QueryPlanNode, ExecutionStep> _nodeToStep = new();
    private readonly HashSet<ISelectionSet> _selectionSets = new();
    private readonly HashSet<ExecutionStep> _completed = new();
    private readonly string _opName;
    private QueryPlanNode? _rootNode;
    private int _opId;
    private int _nodeId;

    public QueryPlanContext(IOperation operation)
    {
        Operation = operation;
        _opName = operation.Name ?? "Remote_" + Guid.NewGuid().ToString("N");
    }

    public IOperation Operation { get; }

    public ExportDefinitionRegistry Exports { get; } = new();

    public List<ExecutionStep> Steps { get; } = new();

    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    public HashSet<VariableDefinitionNode> ForwardedVariables { get; } =
        new(SyntaxComparer.BySyntax);

    public bool HasIntrospectionSelections { get; set; }

    public bool HasHandledSpecialQueryFields { get; set; }

    public NameNode CreateRemoteOperationName()
        => new($"{_opName}_{++_opId}");

    public int NextNodeId() => ++_nodeId;

    public NodeAndStep[] NextBatch()
    {
        if (_completed.Count == 0)
        {
            return _stepToNode
                .Where(t => t.Key.DependsOn.Count == 0)
                .Select(t => new NodeAndStep(t.Value, t.Key))
                .ToArray();
        }

        return _stepToNode
            .Where(t => _completed.IsSupersetOf(t.Key.DependsOn))
            .Select(t => new NodeAndStep(t.Value, t.Key))
            .ToArray();
    }

    public NodeAndStep GetSubscribeRoot()
    {
        var item = _stepToNode.Single(t => t.Value.Kind is QueryPlanNodeKind.Subscribe);
        return new NodeAndStep(item.Value, item.Key);
    }

    public void RegisterNode(QueryPlanNode node, ExecutionStep step)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _stepToNode.Add(step, node);
        _nodeToStep.Add(node, step);
    }

    public void RegisterSelectionSet(ISelectionSet selectionSet)
    {
        if (selectionSet is null)
        {
            throw new ArgumentNullException(nameof(selectionSet));
        }

        _selectionSets.Add(selectionSet);
    }

    public bool TryGetExecutionSteps(
        QueryPlanNode node,
        [NotNullWhen(true)] out ExecutionStep? step)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return _nodeToStep.TryGetValue(node, out step);
    }

    public void SetRootNode(QueryPlanNode rootNode)
    {
        if (_rootNode is not null)
        {
            throw new InvalidOperationException(
                "The root node can only be set once.");
        }

        _rootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
    }

    public void Complete(ExecutionStep step)
    {
        _completed.Add(step);

        if (_stepToNode.Remove(step, out var node))
        {
            _nodeToStep.Remove(node);
        }
    }

    public void Complete(QueryPlanNode node)
    {
        if (_nodeToStep.Remove(node, out var step))
        {
            _stepToNode.Remove(step);
            _completed.Add(step);
        }
    }

    public QueryPlan BuildQueryPlan()
    {
        if (_rootNode is null)
        {
            throw new InvalidOperationException(
                "In order to build a query plan a root node must be set.");
        }

        return new QueryPlan(
            Operation,
            _rootNode,
            Exports.All
                .GroupBy(t => t.SelectionSet)
                .ToDictionary(t => t.Key, t => t.Select(x => x.StateKey).ToArray()),
            _selectionSets);
    }
}
