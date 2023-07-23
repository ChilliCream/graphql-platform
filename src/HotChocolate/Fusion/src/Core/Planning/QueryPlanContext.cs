using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanContext(IOperation operation)
{
    private readonly Dictionary<ExecutionStep, QueryPlanNode> _stepToNode = new();
    private readonly Dictionary<QueryPlanNode, ExecutionStep> _nodeToStep = new();
    private readonly Dictionary<object, SelectionExecutionStep> _selectionLookup = new();
    private readonly HashSet<ISelectionSet> _selectionSets = new();
    private readonly HashSet<ExecutionStep> _completed = new();
    private readonly string _opName = operation.Name ?? "Remote_" + Guid.NewGuid().ToString("N");
    private QueryPlanNode? _rootNode;
    private int _opId;
    private int _stepId;
    private int _nodeId;

    public IOperation Operation { get; } = operation;

    public ExportDefinitionRegistry Exports { get; } = new();

    public List<ExecutionStep> Steps { get; } = new();

    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    public HashSet<VariableDefinitionNode> ForwardedVariables { get; } =
        new(SyntaxComparer.BySyntax);

    public Dictionary<ISelection, ISelection> ParentSelections { get; } = new();

    public bool HasIntrospectionSelections { get; set; }

    public bool HasHandledSpecialQueryFields { get; set; }

    public NameNode CreateRemoteOperationName()
        => new($"{_opName}_{++_opId}");

    public int NextStepId() => ++_stepId;

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

    public IEnumerable<NodeAndStep> AllNodes()
        => _stepToNode.Select(t => new NodeAndStep(t.Value, t.Key));

    public NodeAndStep GetSubscribeRoot()
    {
        var item = _stepToNode.Single(t => t.Value.Kind is QueryPlanNodeKind.Subscribe);
        return new NodeAndStep(item.Value, item.Key);
    }

    public void RegisterNode(QueryPlanNode node, ExecutionStep step)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(step);

        _stepToNode.Add(step, node);
        _nodeToStep.Add(node, step);
    }

    public void RegisterSelectionSet(ISelectionSet selectionSet)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        _selectionSets.Add(selectionSet);
    }

    public bool TryGetExecutionStep(
        QueryPlanNode node,
        [NotNullWhen(true)] out ExecutionStep? step)
    {
        ArgumentNullException.ThrowIfNull(node);

        return _nodeToStep.TryGetValue(node, out step);
    }

    public bool TryGetExecutionStep(
        ISelection selection,
        [NotNullWhen(true)] out SelectionExecutionStep? step)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return _selectionLookup.TryGetValue(selection, out step);
    }

    public bool TryGetExecutionStep(
        ISelectionSet selectionSet,
        [NotNullWhen(true)] out SelectionExecutionStep? step)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        return _selectionLookup.TryGetValue(selectionSet, out step);
    }

    public void ReBuildSelectionLookup()
    {
        _selectionLookup.Clear();

        foreach (var executionStep in Steps)
        {
            if (executionStep is SelectionExecutionStep ses)
            {
                foreach (var selection in ses.AllSelections)
                {
                    _selectionLookup.TryAdd(selection, ses);
                }

                foreach (var selectionSet in ses.AllSelectionSets)
                {
                    _selectionLookup.TryAdd(selectionSet, ses);
                }
            }
        }
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

        _rootNode.Seal();
        return new QueryPlan(Operation, _rootNode, _selectionSets, Exports.All);
    }
}
