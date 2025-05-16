namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClient
{
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
