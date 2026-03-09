namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring a InMemory queue.
/// </summary>
public interface IInMemoryQueueDescriptor : IMessagingDescriptor<InMemoryQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Name(string name);
}
