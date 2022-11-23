using System;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions.InMemory;

internal interface IEventTopic : IDisposable
{
    ValueTask CompleteAsync();
}
