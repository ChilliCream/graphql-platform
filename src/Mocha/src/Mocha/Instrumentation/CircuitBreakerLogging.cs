using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Polly.CircuitBreaker;

namespace Mocha;

internal static class CircuitBreakerLogging
{
    public static CircuitBreakerStrategyOptions WithLogging(
        this CircuitBreakerStrategyOptions options,
        ILogger logger,
        ReceiveMiddlewareFactoryContext context,
        string circuit)
    {
        var endpoint = context.Endpoint.Name;
        var logContext = new Dictionary<string, object?>
        {
            ["InstanceId"] = context.Services.GetService<IHostInfo>()?.InstanceId,
            ["Transport"] = context.Transport.Name,
            ["Endpoint"] = endpoint,
            ["Circuit"] = circuit
        };
        options.OnOpened = args =>
        {
            using (logger.BeginScope(logContext))
            {
                logger.CircuitOpen(args.Outcome.Exception, endpoint, args.BreakDuration);
            }

            return default;
        };
        options.OnClosed = _ =>
        {
            using (logger.BeginScope(logContext))
            {
                logger.CircuitClosed(endpoint);
            }

            return default;
        };
        options.OnHalfOpened = _ =>
        {
            using (logger.BeginScope(logContext))
            {
                logger.CircuitHalfOpen(endpoint);
            }

            return default;
        };
        return options;
    }
}
