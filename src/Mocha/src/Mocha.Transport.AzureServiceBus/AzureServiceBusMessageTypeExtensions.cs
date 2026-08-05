namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods for configuring Azure Service Bus native message properties on message type descriptors.
/// </summary>
public static class AzureServiceBusMessageTypeExtensions
{
    /// <summary>
    /// Configures a <c>SessionId</c> extractor for this message type. Required for messages routed to
    /// session-aware queues or subscriptions. Returning <c>null</c> produces no <c>SessionId</c>.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="extractor">A function that extracts the session identifier from a message instance.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor UseAzureServiceBusSessionId<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor)
    {
        var features = descriptor.Extend().Configuration.Features;

        features.Set(AzureServiceBusSessionIdExtractor.Create(extractor));

        return descriptor;
    }

    /// <summary>
    /// Configures a <c>PartitionKey</c> extractor for this message type. When a <c>SessionId</c> is also
    /// configured, the <c>PartitionKey</c> must equal it; otherwise dispatch fails fast with an
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="extractor">A function that extracts the partition key from a message instance.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor UseAzureServiceBusPartitionKey<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor)
    {
        var features = descriptor.Extend().Configuration.Features;

        features.Set(AzureServiceBusPartitionKeyExtractor.Create(extractor));

        return descriptor;
    }

    /// <summary>
    /// Configures a <c>ReplyToSessionId</c> extractor for this message type. Set this on the request
    /// type so replies land on a specific session in the reply queue.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="extractor">A function that extracts the reply-to session identifier from a message instance.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor UseAzureServiceBusReplyToSessionId<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor)
    {
        var features = descriptor.Extend().Configuration.Features;

        features.Set(AzureServiceBusReplyToSessionIdExtractor.Create(extractor));

        return descriptor;
    }

    /// <summary>
    /// Configures a logical <c>To</c> extractor for this message type, useful for autoforward chains.
    /// Reserved by the broker; consumers may inspect it but it is not currently used for routing.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="extractor">A function that extracts the to-address from a message instance.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor UseAzureServiceBusTo<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor)
    {
        var features = descriptor.Extend().Configuration.Features;

        features.Set(AzureServiceBusToExtractor.Create(extractor));

        return descriptor;
    }
}
