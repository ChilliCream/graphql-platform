using System;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscription
        : IDisposable
    {
        event EventHandler Completed;

        string Id { get; }
    }
}
