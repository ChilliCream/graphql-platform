using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;

namespace Mocha;

/// <summary>
/// Factory for creating the transport-level circuit breaker middleware configuration,
/// which applies Polly-based circuit breaking using the transport's configured options.
/// </summary>
public static class TransportCircuitBreakerMiddleware
{
    /// <summary>
    /// Creates the middleware configuration for the transport circuit breaker middleware.
    /// </summary>
    /// <returns>A receive middleware configuration that creates the transport circuit breaker.</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var circuitBreaker = context.Transport.Options.CircuitBreaker;
                var breakDuration = circuitBreaker.BreakDuration;

                var pipeline = new ResiliencePipelineBuilder()
                    .AddCircuitBreaker(
                        new CircuitBreakerStrategyOptions
                        {
                            FailureRatio = circuitBreaker.FailureRatio,
                            MinimumThroughput = circuitBreaker.MinimumThroughput,
                            SamplingDuration = circuitBreaker.SamplingDuration,
                            BreakDuration = circuitBreaker.BreakDuration
                        })
                    .Build();

                var timeProvider = context.Services.GetRequiredService<TimeProvider>();

                var middleware = new ReceiveCircuitBreakerMiddleware(pipeline, breakDuration, timeProvider);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "TransportCircuitBreaker");
}
