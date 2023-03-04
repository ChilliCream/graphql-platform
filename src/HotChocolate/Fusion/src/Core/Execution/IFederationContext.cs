using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal interface IFederationContext
{
    FusionGraphConfiguration ServiceConfig { get; }

    OperationContext OperationContext { get; }

    ISchema Schema => OperationContext.Schema;

    ResultBuilder Result => OperationContext.Result;

    IOperation Operation => OperationContext.Operation;

    QueryPlan QueryPlan { get; }

    IExecutionState State { get; }

    bool NeedsMoreData(ISelectionSet selectionSet);

    Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string schemaName,
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken);
}
