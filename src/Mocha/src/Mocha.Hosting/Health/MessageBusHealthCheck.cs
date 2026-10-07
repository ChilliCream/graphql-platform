using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mocha.Hosting;

internal sealed class MessageBusHealthCheck(
    IMessageBus dispatcher,
    IMessagingRuntime runtime,
    IOptions<MessageBusHealthCheckOptions> options)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var sendOptions = (options.Value.Endpoint ?? GetInstanceEndpoint()) is { } endpoint
            ? new SendOptions { Endpoint = endpoint }
            : SendOptions.Default;

        var request = new HealthRequest("Health Check");
        var response = await dispatcher.RequestAsync(request, sendOptions, cancellationToken);

        return response.Message == "OK"
            ? HealthCheckResult.Healthy("Message Bus is healthy.")
            : HealthCheckResult.Unhealthy("Message Bus is unhealthy.");
    }

    private Uri? GetInstanceEndpoint()
    {
        if (runtime is not MessagingRuntime messagingRuntime)
        {
            return null;
        }

        foreach (var route in messagingRuntime.Router.InboundRoutes)
        {
            if (route is { IsInstanceScoped: true, Endpoint: { } endpoint }
                && route.Consumer?.Identity == typeof(HealthRequestHandler))
            {
                return endpoint.Source.Address;
            }
        }

        return null;
    }
}
