using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class FederatedQueryContext
{
    public FederatedQueryContext(
        OperationContext operationContext,
        QueryPlan plan,
        IReadOnlySet<ISelectionSet> requiresFetch)
    {
        OperationContext = operationContext;
        Plan = plan;
        RequiresFetch = requiresFetch;
    }

    public OperationContext OperationContext { get; }

    public ISchema Schema => OperationContext.Schema;

    public ResultBuilder Result => OperationContext.Result;

    public IOperation Operation => OperationContext.Operation;

    public QueryPlan Plan { get; }

    public IReadOnlySet<ISelectionSet> RequiresFetch { get; }

    public List<WorkItem> Fetch { get; } = new();

    public Queue<WorkItem> Compose { get; } = new();
}
