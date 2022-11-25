namespace HotChocolate.Subscriptions.InMemory;

/// <summary>
/// This helper interface allows us to invoke TryComplete on a topic
/// without the need to deal with the generic type of the topic.
/// </summary>
internal interface IInMemoryTopic : IDisposable
{
    void TryComplete();
}
