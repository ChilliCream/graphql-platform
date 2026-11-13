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
        var scope = new CircuitBreakerLogScope(
            context.Services.GetService<IHostInfo>()?.InstanceId,
            context.Transport.Name,
            endpoint,
            circuit);
        options.OnOpened = args =>
        {
            using (logger.BeginScope(scope))
            {
                logger.CircuitOpen(args.Outcome.Exception, circuit, endpoint, args.BreakDuration);
            }

            return default;
        };
        options.OnClosed = _ =>
        {
            using (logger.BeginScope(scope))
            {
                logger.CircuitClosed(circuit, endpoint);
            }

            return default;
        };
        options.OnHalfOpened = _ =>
        {
            using (logger.BeginScope(scope))
            {
                logger.CircuitHalfOpen(circuit, endpoint);
            }

            return default;
        };
        return options;
    }

    private sealed class CircuitBreakerLogScope(
        Guid? instanceId,
        string transport,
        string endpoint,
        string circuit) : LogScope
    {
        public override int Count => 4;

        public override KeyValuePair<string, object?> this[int index]
            => index switch
            {
                0 => new("InstanceId", instanceId),
                1 => new("Transport", transport),
                2 => new("Endpoint", endpoint),
                3 => new("Circuit", circuit),
                _ => throw ThrowHelper.LogScopeIndexOutOfRange(index)
            };
    }
}
