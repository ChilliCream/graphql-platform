namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring a InMemory exchange.
/// </summary>
public interface IInMemoryTopicTopologyDescriptor : IMessagingDescriptor<InMemoryTopicConfiguration>
{
    /// <summary>
    /// Sets the name of the exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryTopicTopologyDescriptor Name(string name);
}
