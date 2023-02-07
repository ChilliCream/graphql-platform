using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal class ExecutorWarmupService : BackgroundService
{
    private readonly IRequestExecutorResolver _executorResolver;
    private readonly Dictionary<string, WarmupSchemaTask[]> _tasks;
    private CancellationToken _stopping;

    public ExecutorWarmupService(
        IRequestExecutorResolver executorResolver,
        IEnumerable<WarmupSchemaTask> tasks)
    {
        if (tasks is null)
        {
            throw new ArgumentNullException(nameof(tasks));
        }

        _executorResolver = executorResolver ??
            throw new ArgumentNullException(nameof(executorResolver));
        _tasks = tasks.GroupBy(t => t.SchemaName).ToDictionary(t => t.Key, t => t.ToArray());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stopping = stoppingToken;
        _executorResolver.RequestExecutorEvicted += (_, args) => BeginWarmup(args.Name);

        foreach (var task in _tasks)
        {
            // initialize services
            var executor = await _executorResolver.GetRequestExecutorAsync(task.Key, stoppingToken);

            // execute startup task
            foreach (var warmup in task.Value)
            {
                await warmup.ExecuteAsync(executor, stoppingToken);
            }
        }
    }

    private void BeginWarmup(string schemaName)
    {
        if (_tasks.TryGetValue(schemaName, out var value) && value.Any(t => t.KeepWarm))
        {
            Task.Factory.StartNew(() => WarmupAsync(schemaName, value, _stopping), _stopping);
        }
    }

    private async Task WarmupAsync(
        string schemaName,
        WarmupSchemaTask[] tasks,
        CancellationToken ct)
    {
        // initialize services
        var executor = await _executorResolver.GetRequestExecutorAsync(schemaName, ct);

        // execute startup task
        foreach (var warmup in tasks)
        {
            await warmup.ExecuteAsync(executor, ct);
        }
    }
}
