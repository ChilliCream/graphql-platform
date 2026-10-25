using HotChocolate.Execution;

namespace HotChocolate.Adapters.Mcp;

internal sealed class McpStorageWarmupTask(McpStorageObserver storageObserver) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await storageObserver.StartAsync(cancellationToken);
    }
}
