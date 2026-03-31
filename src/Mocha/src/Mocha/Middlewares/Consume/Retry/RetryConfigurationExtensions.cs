using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring the retry middleware on message bus builders and consumer descriptors.
/// </summary>
public static class RetryConfigurationExtensions
{
    /// <summary>
    /// Adds retry to the message bus consumer pipeline with default settings.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddRetry(
        this IMessageBusBuilder builder)
    {
        builder.ConfigureFeature(f => f.GetOrSet<RetryFeature>());
        builder.UseConsume(ConsumerMiddlewares.Retry, before: "Instrumentation");
        return builder;
    }

    /// <summary>
    /// Adds retry to the message bus consumer pipeline.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">The action to configure retry options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusBuilder AddRetry(
        this IMessageBusBuilder builder,
        Action<RetryOptions> configure)
    {
        builder.ConfigureFeature(f => f.GetOrSet<RetryFeature>().Configure(configure));
        builder.UseConsume(ConsumerMiddlewares.Retry, before: "Instrumentation");
        return builder;
    }

    /// <summary>
    /// Adds retry to the host-level consumer pipeline with default settings.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRetry(
        this IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(x => x.AddRetry());
        return builder;
    }

    /// <summary>
    /// Adds retry to the host-level consumer pipeline.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure retry options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRetry(
        this IMessageBusHostBuilder builder,
        Action<RetryOptions> configure)
    {
        builder.ConfigureMessageBus(x => x.AddRetry(configure));
        return builder;
    }

    /// <summary>
    /// Adds retry configuration to a specific consumer with default settings.
    /// </summary>
    /// <param name="descriptor">The consumer descriptor to configure.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IConsumerDescriptor AddRetry(
        this IConsumerDescriptor descriptor)
    {
        descriptor.Extend().Configuration.Features.GetOrSet<RetryFeature>();
        return descriptor;
    }

    /// <summary>
    /// Adds retry configuration to a specific consumer.
    /// </summary>
    /// <param name="descriptor">The consumer descriptor to configure.</param>
    /// <param name="configure">The action to configure retry options.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IConsumerDescriptor AddRetry(
        this IConsumerDescriptor descriptor,
        Action<RetryOptions> configure)
    {
        descriptor.Extend().Configuration.Features
            .GetOrSet<RetryFeature>().Configure(configure);
        return descriptor;
    }
}
