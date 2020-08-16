using System;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions.InMemory
{
    internal interface IEventTopic
    {
        event EventHandler<EventArgs>? Unsubscribed;

        ValueTask CompleteAsync();

        Task<bool> TryClose();
    }
}
