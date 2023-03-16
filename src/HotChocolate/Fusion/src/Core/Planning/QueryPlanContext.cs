using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanContext
{
    private readonly string _opName;
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

    public Dictionary<ExecutionStep, QueryPlanNode> Nodes { get; } = new();

    public HashSet<VariableDefinitionNode> ForwardedVariables { get; } =
        new(SyntaxComparer.BySyntax);

    public HashSet<ISelectionSet> HasNodes { get; } = new();

    public bool HasIntrospectionSelections { get; set; }

    public NameNode CreateRemoteOperationName()
        => new($"{_opName}_{++_opId}");

    public int CreateNodeId() => ++_nodeId;

    public QueryPlan? Plan { get; set; }
}
