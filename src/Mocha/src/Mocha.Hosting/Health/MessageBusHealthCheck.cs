using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Events;

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

        if (GetTransportHealthFailure(failureStatus) is { } unhealthy)
        {
            return unhealthy;
        }

        var timeout = options.Timeout;
        var sendOptions = new SendOptions
        {
            Endpoint = options.Endpoint,
            ExpirationTime = timeProvider.GetUtcNow().Add(timeout)
        };

        HealthResponse response;
        using var timeoutSource = new CancellationTokenSource(timeout, timeProvider);
        using var requestSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            var request = new HealthRequest("Health Check");
            response = await dispatcher.RequestAsync(request, sendOptions, requestSource.Token);
        }
        catch (OperationCanceledException)
            when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(failureStatus, CreateTimeoutDescription(timeout));
        }
        catch (ResponseTimeoutException ex)
        {
            return new HealthCheckResult(failureStatus, CreateTimeoutDescription(ex.Timeout), ex);
        }

        if (GetTransportHealthFailure(failureStatus) is { } failedDuringRequest)
        {
            return failedDuringRequest;
        }

        return response.Message == "OK"
            ? HealthCheckResult.Healthy("Message Bus is healthy.")
            : new HealthCheckResult(failureStatus, "Message Bus is unhealthy.");
    }

    private HealthCheckResult? GetTransportHealthFailure(HealthStatus failureStatus)
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

    private static string CreateTimeoutDescription(TimeSpan timeout)
        => $"Message Bus did not answer the health request within {timeout}.";
}
