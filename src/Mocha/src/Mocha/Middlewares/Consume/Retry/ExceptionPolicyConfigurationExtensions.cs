using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring exception policies including retry, redelivery,
/// and per-exception rules on message bus builders, host builders, descriptors, and consumers.
/// </summary>
public static class ExceptionPolicyConfigurationExtensions
{
    /// <summary>
    /// Adds exception policy configuration to the message bus with default settings.
    /// Registers a catch-all rule for <see cref="Exception"/> with default retry and redelivery.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddExceptionPolicy(this IMessageBusBuilder builder)
    {
        builder.ConfigureFeature(f => f.GetOrSet<ExceptionPolicyFeature>()
            .Configure(p => p.Default().Retry().ThenRedeliver()));
        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the message bus.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddExceptionPolicy(
        this IMessageBusBuilder builder,
        Action<ExceptionPolicyOptions> configure)
    {
        builder.ConfigureFeature(f => f.GetOrSet<ExceptionPolicyFeature>().Configure(configure));
        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the host-level message bus with default settings.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddExceptionPolicy(this IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(x => x.AddExceptionPolicy());
        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the host-level message bus.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddExceptionPolicy(
        this IMessageBusHostBuilder builder,
        Action<ExceptionPolicyOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddExceptionPolicy(configure));
        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to a specific descriptor (e.g., receive endpoint or transport)
    /// with default settings.
    /// Registers a catch-all rule for <see cref="Exception"/> with default retry and redelivery.
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddExceptionPolicy<TDescriptor>(
        this TDescriptor descriptor)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<ExceptionPolicyFeature>()
            .Configure(p => p.Default().Retry().ThenRedeliver());
        return descriptor;
    }

    /// <summary>
    /// Adds exception policy configuration to a specific descriptor (e.g., receive endpoint or transport).
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddExceptionPolicy<TDescriptor>(
        this TDescriptor descriptor,
        Action<ExceptionPolicyOptions> configure)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor.Extend().Configuration.Features.GetOrSet<ExceptionPolicyFeature>().Configure(configure);
        return descriptor;
    }

    /// <summary>
    /// Adds exception policy configuration to a specific consumer with default settings.
    /// Registers a catch-all rule for <see cref="Exception"/> with default retry and redelivery.
    /// </summary>
    /// <param name="descriptor">The consumer descriptor to configure.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IConsumerDescriptor AddExceptionPolicy(this IConsumerDescriptor descriptor)
    {
        descriptor.Extend().Configuration.Features.GetOrSet<ExceptionPolicyFeature>()
            .Configure(p => p.Default().Retry().ThenRedeliver());
        return descriptor;
    }

    /// <summary>
    /// Adds exception policy configuration to a specific consumer.
    /// </summary>
    /// <param name="descriptor">The consumer descriptor to configure.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IConsumerDescriptor AddExceptionPolicy(
        this IConsumerDescriptor descriptor,
        Action<ExceptionPolicyOptions> configure)
    {
        descriptor.Extend().Configuration.Features.GetOrSet<ExceptionPolicyFeature>().Configure(configure);
        return descriptor;
    }
}
