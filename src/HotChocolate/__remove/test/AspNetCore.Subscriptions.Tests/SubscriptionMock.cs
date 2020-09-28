using System;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionMock
        : ISubscription
    {
        public string Id { get; set; } = "abc";

        public bool IsDisposed { get; private set; }

        public event EventHandler Completed;

        public void Complete()
        {
            Completed.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
