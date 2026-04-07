using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring exception policies including retry, redelivery,
/// and per-exception rules on message bus builders, host builders, and descriptors.
/// </summary>
public static class ResilienceConfigurationExtensions
{
    /// <summary>
    /// Adds exception policy configuration to the message bus with default settings.
    /// Registers a catch-all rule for <see cref="Exception"/> with default retry and redelivery.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddResilience(this IMessageBusBuilder builder)
    {
        builder.ConfigureFeature(f => f.GetOrSet<ExceptionPolicyFeature>().Configure(p => p.AddDefaultPolicy()));

        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the message bus.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddResilience(
        this IMessageBusBuilder builder,
        Action<ExceptionPolicyOptions> configure)
    {
        builder.ConfigureFeature(f =>
        {
            var feature = f.GetOrSet<ExceptionPolicyFeature>();
            feature.Configure(p => p.AddDefaultPolicy());
            feature.Configure(configure);
        });

        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the host-level message bus with default settings.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddResilience(this IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(x => x.AddResilience());
        return builder;
    }

    /// <summary>
    /// Adds exception policy configuration to the host-level message bus.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddResilience(
        this IMessageBusHostBuilder builder,
        Action<ExceptionPolicyOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddResilience(configure));
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
    public static TDescriptor AddResilience<TDescriptor>(this TDescriptor descriptor)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        descriptor
            .Extend()
            .Configuration.Features.GetOrSet<ExceptionPolicyFeature>()
            .Configure(p => p.AddDefaultPolicy());

        return descriptor;
    }

    /// <summary>
    /// Adds exception policy configuration to a specific descriptor (e.g., receive endpoint or transport).
    /// </summary>
    /// <typeparam name="TDescriptor">The descriptor type that supports receive middleware.</typeparam>
    /// <param name="descriptor">The descriptor to configure.</param>
    /// <param name="configure">The action to configure exception policy options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static TDescriptor AddResilience<TDescriptor>(
        this TDescriptor descriptor,
        Action<ExceptionPolicyOptions> configure)
        where TDescriptor : IReceiveMiddlewareProvider
    {
        var feature = descriptor.Extend().Configuration.Features.GetOrSet<ExceptionPolicyFeature>();
        feature.Configure(p => p.AddDefaultPolicy());
        feature.Configure(configure);

        return descriptor;
    }

    private static ExceptionPolicyOptions AddDefaultPolicy(this ExceptionPolicyOptions options)
    {
        options.Default().Retry().ThenRedeliver();
        return options;
    }
}
