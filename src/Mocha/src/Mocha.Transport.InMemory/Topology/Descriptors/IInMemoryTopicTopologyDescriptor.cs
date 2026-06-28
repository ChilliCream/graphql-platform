namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory topic topology entity.
/// </summary>
public interface IInMemoryTopicTopologyDescriptor : IMessagingDescriptor<InMemoryTopicConfiguration>
{
    /// <summary>
    /// Sets the name of the topic.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryTopicTopologyDescriptor Name(string name);
}
