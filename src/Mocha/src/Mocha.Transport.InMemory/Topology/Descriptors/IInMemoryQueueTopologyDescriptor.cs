namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring a InMemory queue.
/// </summary>
public interface IInMemoryQueueTopologyDescriptor : IMessagingDescriptor<InMemoryQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueTopologyDescriptor Name(string name);
}
