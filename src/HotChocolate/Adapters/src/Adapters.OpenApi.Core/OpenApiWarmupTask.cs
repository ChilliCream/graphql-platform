using HotChocolate.Execution;
using HotChocolate.Adapters.OpenApi.Validation;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiWarmupTask(OpenApiDocumentManager manager) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await manager.UpdateSchemaAsync(executor.Schema, cancellationToken);
    }
}
