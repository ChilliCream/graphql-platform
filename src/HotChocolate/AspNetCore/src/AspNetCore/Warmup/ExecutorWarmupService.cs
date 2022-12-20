using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal class ExecutorWarmupService : BackgroundService
{
    private readonly IRequestExecutorResolver _executorResolver;
    private readonly HashSet<string> _schemaNames;

    public ExecutorWarmupService(
        IRequestExecutorResolver executorResolver,
        IEnumerable<WarmupSchema> schemas)
    {
        if (schemas is null)
        {
            throw new ArgumentNullException(nameof(schemas));
        }

        _executorResolver = executorResolver ??
            throw new ArgumentNullException(nameof(executorResolver));
        _schemaNames = new HashSet<string>(schemas.Select(t => t.SchemaName));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var schemaName in _schemaNames)
        {
            // initialize services
            var executor =
                await _executorResolver.GetRequestExecutorAsync(schemaName, stoppingToken);

            // initialize pipeline with warmup request
            IQueryRequest warmupRequest = QueryRequestBuilder.New()
                .SetQuery("{ __typename }")
                .AllowIntrospection()
                .Create();

            await executor.ExecuteAsync(warmupRequest, stoppingToken);
        }
    }
}
