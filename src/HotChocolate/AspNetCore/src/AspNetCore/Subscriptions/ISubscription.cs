using System;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface ISubscription : IDisposable
    {
        event EventHandler? Completed;

        string Id { get; }
    }
}
