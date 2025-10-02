using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal sealed class RequestExecutorWarmupService(
    IRequestExecutorOptionsMonitor executorOptionsMonitor,
    IRequestExecutorProvider provider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var warmupTasks = new List<Task>();

        foreach (var schemaName in provider.SchemaNames)
        {
            // TODO: Maybe this isn't the best approach...
            var options = await executorOptionsMonitor.GetAsync(schemaName, cancellationToken);
            // var setup = optionsMonitor.Get(schemaName);
            //
            // var requestOptions = FusionRequestExecutorManager.CreateRequestOptions(setup);

            // if (!requestOptions.LazyInitialization)
            // {
            //     var warmupTask = WarmupAsync(schemaName, cancellationToken);
            //     warmupTasks.Add(warmupTask);
            // }
        }

        await Task.WhenAll(warmupTasks).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WarmupAsync(string schemaName, CancellationToken cancellationToken)
    {
        await provider.GetExecutorAsync(schemaName, cancellationToken).ConfigureAwait(false);
    }
}
