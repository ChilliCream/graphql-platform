using System;
using System.Security.Claims;
using HotChocolate.Execution.Processing;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionSessionMock : ISubscriptionSession
    {
        public string Id { get; set; } = "abc";

        public ISubscription Subscription => throw new NotImplementedException();

        public bool IsDisposed { get; private set; }

        public event EventHandler Completed;

        public void Complete()
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
