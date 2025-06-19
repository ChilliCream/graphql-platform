namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClient : IAsyncDisposable
{
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
