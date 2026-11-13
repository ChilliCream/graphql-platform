using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mocha.Hosting;

internal sealed class MessageBusHealthCheck(
    IMessageBus dispatcher,
    IMessagingRuntime runtime,
    TimeProvider timeProvider,
    MessageBusHealthCheckOptions options)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failureStatus = context.Registration.FailureStatus;

        if (runtime.GetTransportHealthFailure(failureStatus) is { } unhealthy)
        {
            return unhealthy;
        }

        // The registration timeout cancels the request. The request expires at the same time.
        var timeout = context.Registration.Timeout;
        var sendOptions = new SendOptions
        {
            Endpoint = options.Endpoint,
            ExpirationTime = timeout > TimeSpan.Zero ? timeProvider.GetUtcNow().Add(timeout) : null
        };

        var request = new HealthRequest("Health Check");
        var response = await dispatcher.RequestAsync(request, sendOptions, cancellationToken);

        if (runtime.GetTransportHealthFailure(failureStatus) is { } failedDuringRequest)
        {
            return failedDuringRequest;
        }

        return response.Message == "OK"
            ? HealthCheckResult.Healthy("Message Bus is healthy.")
            : new HealthCheckResult(failureStatus, "Message Bus is unhealthy.");
    }
}

file static class Extensions
{
    /// <summary>
    /// Creates the failed result that lists every unhealthy transport, or <c>null</c> when all are healthy.
    /// </summary>
    public static HealthCheckResult? GetTransportHealthFailure(
        this IMessagingRuntime runtime,
        HealthStatus failureStatus)
    {
        string? description = null;
        Dictionary<string, object>? reasons = null;

        foreach (var transport in runtime.Transports)
        {
            if (transport.HealthFailureReason is { } reason)
            {
                description ??= $"Transport '{transport.Name}' is unhealthy. {reason}";
                reasons ??= [];
                reasons[transport.Name] = reason;
            }
        }

        return description is null
            ? null
            : new HealthCheckResult(failureStatus, description, exception: null, reasons);
    }
}
