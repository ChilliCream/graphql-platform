using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanContext
{
    private readonly string _opName;
    private int _opId;

    public QueryPlanContext(IOperation operation)
    {
        Operation = operation;
        _opName = operation.Name ?? "Remote_" + Guid.NewGuid().ToString("N");
    }

    public IOperation Operation { get; }

    public ExportDefinitionRegistry Exports { get; } = new();

    public List<IExecutionStep> Steps { get; } = new();

    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    public Dictionary<IExecutionStep, RequestNode> RequestNodes { get; } = new();

    public bool HasIntrospectionSelections { get; set; }

    public NameNode CreateRemoteOperationName()
        => new($"{_opName}_{++_opId}");
}
