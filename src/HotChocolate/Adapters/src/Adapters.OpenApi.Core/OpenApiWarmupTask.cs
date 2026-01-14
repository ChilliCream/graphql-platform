using HotChocolate.Execution;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiWarmupTask(OpenApiDefinitionRegistry registry) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await registry.UpdateSchemaAsync(executor.Schema, cancellationToken);
    }
}
