using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha;

/// <summary>
/// Hosted service that automatically starts the messaging runtime when the host starts.
/// </summary>
internal sealed class MessagingRuntimeHostedService(IMessagingRuntime runtime) : IHostedService
{
    private readonly MessagingRuntime _runtime = (MessagingRuntime)runtime;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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
