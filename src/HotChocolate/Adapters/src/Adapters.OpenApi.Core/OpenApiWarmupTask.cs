using HotChocolate.Execution;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiWarmupTask(OpenApiDocumentManager manager) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await manager.UpdateSchemaAsync(executor.Schema, cancellationToken);
    }
}
