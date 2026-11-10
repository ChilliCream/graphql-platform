using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mocha.Hosting;

internal sealed class MessageBusHealthCheck(
    IMessageBus dispatcher,
    IOptions<MessageBusHealthCheckOptions> options,
    IMessagingRuntime runtime)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (GetTransportHealthFailure() is { } unhealthy)
        {
            return unhealthy;
        }

        var sendOptions = options.Value.Endpoint is { } endpoint
            ? new SendOptions { Endpoint = endpoint }
            : SendOptions.Default;

        var request = new HealthRequest("Health Check");
        var response = await dispatcher.RequestAsync(request, sendOptions, cancellationToken);

        return GetTransportHealthFailure() ?? (response.Message == "OK"
            ? HealthCheckResult.Healthy("Message Bus is healthy.")
            : HealthCheckResult.Unhealthy("Message Bus is unhealthy."));
    }

    private HealthCheckResult? GetTransportHealthFailure()
    {
        foreach (var transport in runtime.Transports)
        {
            if (!transport.IsHealthy)
            {
                var description = $"Transport '{transport.Name}' is unhealthy.";
                if (transport.HealthFailureReason is { } reason)
                {
                    description += $" {reason}";
                }

                return HealthCheckResult.Unhealthy(description);
            }
        }

        return null;
    }
}
