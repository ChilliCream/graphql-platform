using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class FederatedQueryContext
{
    public FederatedQueryContext(
        ISchema schema,
        ResultBuilder result,
        IOperation operation,
        QueryPlan plan,
        IReadOnlySet<ISelectionSet> requiresFetch)
    {
        Schema = schema;
        Result = result;
        Operation = operation;
        Plan = plan;
        RequiresFetch = requiresFetch;
    }

    public ISchema Schema { get; }

    public ResultBuilder Result { get; }

    public IOperation Operation { get; }

    public QueryPlan Plan { get; }

    public IReadOnlySet<ISelectionSet> RequiresFetch { get; }

    public List<WorkItem> Fetch { get; } = new();

    public Queue<WorkItem> Compose { get; } = new();
}
