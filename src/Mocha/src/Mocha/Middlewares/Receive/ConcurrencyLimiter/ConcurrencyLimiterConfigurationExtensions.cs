using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring the concurrency limiter middleware on message bus builders and descriptors.
/// </summary>
public static class ConcurrencyLimiterConfigurationExtensions
{
    /// <summary>
    /// Adds a concurrency limiter to the message bus receive pipeline.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure concurrency limiter options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddConcurrencyLimiter(
        this IMessageBusBuilder builder,
        Action<ConcurrencyLimiterOptions> configure)
    {
        builder.ConfigureFeature(f => f.GetOrSet<ConcurrencyLimiterFeature>().Configure(configure));
        return builder;
    }

    /// <summary>
    /// Adds a concurrency limiter to the host-level receive pipeline.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure concurrency limiter options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddConcurrencyLimiter(
        this IMessageBusHostBuilder builder,
        Action<ConcurrencyLimiterOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddConcurrencyLimiter(configure));

        return builder;
    }

    /// <summary>
    /// Adds a concurrency limiter to the receive pipeline of a specific descriptor.
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <param name="configure">The action to configure concurrency limiter options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddConcurrencyLimiter<TDescriptor>(
        this TDescriptor descriptor,
        Action<ConcurrencyLimiterOptions> configure)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<ConcurrencyLimiterFeature>().Configure(configure);

        return descriptor;
    }
}
