using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Health check that verifies the Event Hub transport's receive endpoints have running processors.
/// </summary>
public sealed class EventHubHealthCheck : IHealthCheck
{
    private readonly EventHubMessagingTransport _transport;

    /// <summary>
    /// Creates a new Event Hub health check for the specified transport instance.
    /// </summary>
    /// <param name="transport">The Event Hub transport to monitor.</param>
    public EventHubHealthCheck(EventHubMessagingTransport transport)
    {
        _transport = transport;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var receiveEndpoints = _transport.ReceiveEndpoints
            .OfType<EventHubReceiveEndpoint>()
            .ToList();

        if (receiveEndpoints.Count == 0)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("No Event Hub receive endpoints configured."));
        }

        var runningCount = 0;
        var stoppedEndpoints = new List<string>();

        foreach (var endpoint in receiveEndpoints)
        {
            if (endpoint.IsProcessorRunning)
            {
                runningCount++;
            }
            else
            {
                stoppedEndpoints.Add(endpoint.Name);
            }
        }

        if (stoppedEndpoints.Count == 0)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy(
                    $"All {runningCount} Event Hub processor(s) are running."));
        }

        var data = new Dictionary<string, object>
        {
            ["running"] = runningCount,
            ["stopped"] = stoppedEndpoints.Count,
            ["stoppedEndpoints"] = stoppedEndpoints
        };

        if (runningCount > 0)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    $"{stoppedEndpoints.Count} of {receiveEndpoints.Count} Event Hub processor(s) are not running.",
                    data: data));
        }

        return Task.FromResult(
            HealthCheckResult.Unhealthy(
                "No Event Hub processors are running.",
                data: data));
    }
}
