namespace HotChocolate.Fusion.Execution;

public interface IRemoteRequestExecutor
{
    string SchemaName { get; }

    Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken);
}
