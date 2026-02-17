using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClient : IAsyncDisposable
{
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        ExecutionNode node,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
