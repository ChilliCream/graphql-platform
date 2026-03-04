using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha;

/// <summary>
/// Hosted service that automatically starts the messaging runtime when the host starts.
/// </summary>
internal sealed class MessagingRuntimeHostedService(IServiceProvider services) : IHostedService
{
    private MessagingRuntime? _runtime;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
        await _runtime.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_runtime is not null)
        {
            await _runtime.DisposeAsync();
        }
    }
}
