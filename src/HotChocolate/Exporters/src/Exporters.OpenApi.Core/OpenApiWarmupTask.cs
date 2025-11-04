using HotChocolate.Execution;
using HotChocolate.Exporters.OpenApi.Validation;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiWarmupTask(OpenApiDocumentRegistry registry ) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        await registry.InitializeAsync(executor.Schema, cancellationToken);
    }
}
