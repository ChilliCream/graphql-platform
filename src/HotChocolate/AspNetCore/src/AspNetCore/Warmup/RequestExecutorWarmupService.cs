using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal class RequestExecutorWarmupService(
    IRequestExecutorWarmup executorWarmup)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
        => await executorWarmup.WarmupAsync(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
