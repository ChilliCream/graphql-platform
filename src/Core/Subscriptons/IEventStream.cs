using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public interface IEventStream
        : IDisposable
    {
        string SubscriptionId { get; }

        Task NextAsync(CancellationToken cancellationToken = default);

        bool IsCompleted { get; }
    }
}

