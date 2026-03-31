using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring the redelivery middleware on message bus builders and descriptors.
/// </summary>
public static class RedeliveryConfigurationExtensions
{
    /// <summary>
    /// Adds redelivery to the message bus receive pipeline with default settings.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddRedelivery(
        this IMessageBusBuilder builder)
    {
        builder.ConfigureFeature(f => f.GetOrSet<RedeliveryFeature>());
        builder.UseReceive(ReceiveMiddlewares.Redelivery, after: "Fault");
        return builder;
    }

    /// <summary>
    /// Adds redelivery to the message bus receive pipeline.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure redelivery options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddRedelivery(
        this IMessageBusBuilder builder,
        Action<RedeliveryOptions> configure)
    {
        builder.ConfigureFeature(f => f.GetOrSet<RedeliveryFeature>().Configure(configure));
        builder.UseReceive(ReceiveMiddlewares.Redelivery, after: "Fault");
        return builder;
    }

    /// <summary>
    /// Adds redelivery to the host-level receive pipeline with default settings.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRedelivery(
        this IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(x => x.AddRedelivery());
        return builder;
    }

    /// <summary>
    /// Adds redelivery to the host-level receive pipeline.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure redelivery options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRedelivery(
        this IMessageBusHostBuilder builder,
        Action<RedeliveryOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddRedelivery(configure));
        return builder;
    }

    /// <summary>
    /// Adds redelivery configuration to a specific descriptor (e.g., receive endpoint or transport) with default settings.
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddRedelivery<TDescriptor>(
        this TDescriptor descriptor)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<RedeliveryFeature>();
        return descriptor;
    }

    /// <summary>
    /// Adds redelivery configuration to a specific descriptor (e.g., receive endpoint or transport).
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <param name="configure">The action to configure redelivery options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddRedelivery<TDescriptor>(
        this TDescriptor descriptor,
        Action<RedeliveryOptions> configure)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<RedeliveryFeature>().Configure(configure);
        return descriptor;
    }
}
