namespace HotChocolate.Fusion.Execution;

internal sealed class RemoteRequestExecutorFactory
{
    private readonly Dictionary<string, IRemoteRequestExecutor> _executors;

    public RemoteRequestExecutorFactory(IEnumerable<IRemoteRequestExecutor> executors)
    {
        _executors = executors.ToDictionary(t => t.SchemaName);
    }

    public IRemoteRequestExecutor Create(string schemaName)
        => _executors[schemaName];
}
