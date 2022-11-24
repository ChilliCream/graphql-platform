using System;

namespace HotChocolate.Subscriptions.InMemory;

internal interface IInMemoryTopic : IDisposable
{
    void TryComplete();
}