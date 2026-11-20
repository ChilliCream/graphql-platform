using Microsoft.Extensions.DependencyInjection;
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

        var checks = runtime.Services.GetServices<IMessageBusHealthCheck>();

        if (await checks.CheckAsync(failureStatus, cancellationToken) is { } unhealthy)
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

        if (await checks.CheckAsync(failureStatus, cancellationToken) is { } failedDuringRequest)
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
    /// Runs the checks and creates the failed result that lists every reported problem, or <c>null</c> when
    /// no check reports a problem.
    /// </summary>
    public static async ValueTask<HealthCheckResult?> CheckAsync(
        this IEnumerable<IMessageBusHealthCheck> checks,
        HealthStatus failureStatus,
        CancellationToken cancellationToken)
    {
        var context = new MessageBusHealthContext();

        foreach (var check in checks)
        {
            await check.CheckAsync(context, cancellationToken);
        }

        if (context.Failures.Count == 0)
        {
            return null;
        }

        var reasons = new Dictionary<string, object>();

        foreach (var failure in context.Failures)
        {
            reasons.TryAdd(failure.Name, failure.Reason);
        }

        return new HealthCheckResult(failureStatus, context.Failures[0].Reason, exception: null, reasons);
    }
}
