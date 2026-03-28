namespace Mocha.Transport.NATS;

/// <summary>
/// Extension methods for configuring outbound route destinations targeting NATS subjects.
/// </summary>
public static class NatsMessageTypeDescriptorExtensions
{
    /// <summary>
    /// Sets the outbound route destination to a NATS subject using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema for the transport (e.g., "nats").</param>
    /// <param name="subjectName">The target subject name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToNatsSubject(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string subjectName)
        => descriptor.Destination(new Uri($"{schema}:s/{subjectName}"));

    /// <summary>
    /// Sets the outbound route destination to a NATS subject using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="subjectName">The target subject name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToNatsSubject(this IOutboundRouteDescriptor descriptor, string subjectName)
        => descriptor.ToNatsSubject(NatsTransportConfiguration.DefaultSchema, subjectName);
}
