using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring the circuit breaker middleware on message bus builders and descriptors.
/// </summary>
public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>
    /// Adds a circuit breaker to the message bus receive pipeline.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure circuit breaker options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddCircuitBreaker(
        this IMessageBusBuilder builder,
        Action<CircuitBreakerOptions> configure)
    {
        builder.ConfigureFeature(f => f.GetOrSet<CircuitBreakerFeature>().Configure(configure));
        return builder;
    }

    /// <summary>
    /// Adds a circuit breaker to the host-level receive pipeline.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure circuit breaker options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddCircuitBreaker(
        this IMessageBusHostBuilder builder,
        Action<CircuitBreakerOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddCircuitBreaker(configure));
        return builder;
    }

    /// <summary>
    /// Adds a circuit breaker to the receive pipeline of a specific descriptor (e.g., receive endpoint or consumer).
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <param name="configure">The action to configure circuit breaker options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddCircuitBreaker<TDescriptor>(
        this TDescriptor descriptor,
        Action<CircuitBreakerOptions> configure)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<CircuitBreakerFeature>().Configure(configure);

        return descriptor;
    }
}
