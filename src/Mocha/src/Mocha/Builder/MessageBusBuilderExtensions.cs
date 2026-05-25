using Mocha.Events;
using Mocha.Features;

namespace Mocha;

/// <summary>
/// Provides extension methods for <see cref="IMessageBusBuilder"/> to simplify feature
/// configuration and default pipeline setup.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Configures a feature of the specified type, creating it if it does not already exist in the
    /// feature collection.
    /// </summary>
    /// <typeparam name="TFeature">
    /// The feature type to configure. Must have a parameterless constructor.
    /// </typeparam>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="configure">An action to apply settings to the feature instance.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IMessageBusBuilder ConfigureFeature<TFeature>(
        this IMessageBusBuilder builder,
        Action<TFeature> configure)
        where TFeature : new()
    {
        builder.ConfigureFeature(f => configure(f.GetOrSet<TFeature>()));
        return builder;
    }

    internal static void AddDefaults(this MessageBusBuilder builder)
    {
        builder.UseConsume(ConsumerMiddlewares.Retry, before: "Instrumentation");
        builder.UseConsume(ConsumerMiddlewares.Instrumentation);

        builder.UseReceive(ReceiveMiddlewares.TransportCircuitBreaker);
        builder.UseReceive(ReceiveMiddlewares.ConcurrencyLimiter);
        builder.UseReceive(ReceiveMiddlewares.Instrumentation);
        builder.UseReceive(ReceiveMiddlewares.DeadLetter);
        builder.UseReceive(ReceiveMiddlewares.Fault);
        builder.UseReceive(ReceiveMiddlewares.Redelivery, after: "Fault");
        builder.UseReceive(ReceiveMiddlewares.CircuitBreaker);
        builder.UseReceive(ReceiveMiddlewares.Expiry);
        builder.UseReceive(ReceiveMiddlewares.MessageTypeSelection);
        builder.UseReceive(ReceiveMiddlewares.Routing);

        builder.UseDispatch(DispatchMiddlewares.Instrumentation);
        builder.UseDispatch(DispatchMiddlewares.Serialization);

        builder.AddConcurrencyLimiter(o => o.MaxConcurrency = Environment.ProcessorCount * 2);

        builder.AddMessage<NotAcknowledgedEvent>(x =>
        {
            x.AddSerializer(new JsonMessageSerializer(AcknowledgementJsonContext.Default.NotAcknowledgedEvent));
            x.Extend().Configuration.IsInternal = true;
        });

        builder.AddMessage<AcknowledgedEvent>(x =>
        {
            x.AddSerializer(new JsonMessageSerializer(AcknowledgementJsonContext.Default.AcknowledgedEvent));
            x.Extend().Configuration.IsInternal = true;
        });
    }
}
