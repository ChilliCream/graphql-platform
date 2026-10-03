using Microsoft.Extensions.Hosting;

namespace Mocha;

/// <summary>
/// Hosted service that starts and stops the messaging runtime with the host.
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
        await _runtime.StopAsync(cancellationToken);
    }
}
