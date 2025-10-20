using HotChocolate.Execution.Internal;

namespace HotChocolate.AspNetCore.Warmup;

internal sealed class SchemaFileExporterWarmupTask(string schemaFileName) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await SchemaFileExporter.Export(schemaFileName, executor, cancellationToken);
    }
}
