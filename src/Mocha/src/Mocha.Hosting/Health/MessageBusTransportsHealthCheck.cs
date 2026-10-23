using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mocha.Hosting;

internal sealed class MessageBusTransportsHealthCheck(
    IMessagingRuntime runtime,
    TimeProvider timeProvider,
    IOptions<MessageBusTransportsHealthCheckOptions> options)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var unhealthyAfter = options.Value.UnhealthyAfter;
        var status = HealthStatus.Healthy;
        var data = new Dictionary<string, object>();
        var problems = new List<string>();
        Exception? exception = null;

        foreach (var transport in runtime.Transports)
        {
            foreach (var entry in transport.GetHealth().Entries)
            {
                var entryStatus = entry.Status switch
                {
                    TransportHealthStatus.Healthy => HealthStatus.Healthy,
                    TransportHealthStatus.Degraded when now - (entry.Since ?? now) >= unhealthyAfter
                        => HealthStatus.Unhealthy,
                    TransportHealthStatus.Degraded => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };

                var name = $"{transport.Name}:{entry.Name}";
                data[name] = entry.Description;

                if (entryStatus == HealthStatus.Healthy)
                {
                    continue;
                }

                problems.Add($"{name} {entry.Description}");
                exception ??= entry.Exception;

                // HealthStatus orders Unhealthy before Degraded before Healthy.
                if (entryStatus < status)
                {
                    status = entryStatus;
                }
            }
        }

        var description = problems.Count == 0
            ? "Message bus transports are healthy."
            : string.Join("; ", problems);

        return Task.FromResult(new HealthCheckResult(status, description, exception, data));
    }
}
