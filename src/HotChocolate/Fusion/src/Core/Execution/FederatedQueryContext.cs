using System.Collections.Concurrent;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class FederatedQueryContext : IFederationContext
{
    public FederatedQueryContext(
        ServiceConfiguration serviceConfig,
        QueryPlan queryPlan,
        OperationContext operationContext)
    {
        ServiceConfig = serviceConfig ??
            throw new ArgumentNullException(nameof(serviceConfig));
        QueryPlan = queryPlan ??
            throw new ArgumentNullException(nameof(queryPlan));
        OperationContext = operationContext ??
            throw new ArgumentNullException(nameof(operationContext));
    }

    public ServiceConfiguration ServiceConfig { get; }

    public QueryPlan QueryPlan { get; }

    public IExecutionState State { get; } = new ExecutionState();

    public OperationContext OperationContext { get; }

    public ConcurrentQueue<WorkItem> Work { get; } = new();

    public Dictionary<QueryPlanNode, List<WorkItem>> WorkByNode { get; } = new();

    public HashSet<QueryPlanNode> Completed { get; } = new();

    public bool NeedsMoreData(ISelectionSet selectionSet)
        => QueryPlan.HasNodes(selectionSet);

    public Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string schemaName,
        IReadOnlyList<GraphQLRequest> request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void BeginExecution()
    {
    }

    public void CompletedExecution()
    {
    }
}

internal interface IFederationContext
{
    ServiceConfiguration ServiceConfig { get; }

    OperationContext OperationContext { get; }

    ISchema Schema => OperationContext.Schema;

    ResultBuilder Result => OperationContext.Result;

    IOperation Operation => OperationContext.Operation;

    QueryPlan QueryPlan { get; }

    IExecutionState State { get; }

    bool NeedsMoreData(ISelectionSet selectionSet);

    Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string schemaName,
        IReadOnlyList<GraphQLRequest> request,
        CancellationToken cancellationToken);

    void BeginExecution();

    void CompletedExecution();
}
