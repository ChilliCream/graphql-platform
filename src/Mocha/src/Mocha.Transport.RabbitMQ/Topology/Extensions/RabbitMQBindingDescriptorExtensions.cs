namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for configuring RabbitMQ binding descriptors with headers exchange match types.
/// </summary>
public static class RabbitMQBindingDescriptorExtensions
{
    /// <summary>
    /// Sets the match type for binding arguments in headers exchange.
    /// Determines whether all headers must match (All) or any header can match (Any).
    /// </summary>
    /// <param name="descriptor">The binding descriptor.</param>
    /// <param name="type">The match type (All or Any).</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQBindingDescriptor Match(
        this IRabbitMQBindingDescriptor descriptor,
        RabbitMQBindingMatchType type)
    {
        return descriptor.WithArgument("x-match", type == RabbitMQBindingMatchType.All ? "all" : "any");
    }
}
