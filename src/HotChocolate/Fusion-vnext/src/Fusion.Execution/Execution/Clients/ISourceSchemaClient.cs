namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClient : IAsyncDisposable
{
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
