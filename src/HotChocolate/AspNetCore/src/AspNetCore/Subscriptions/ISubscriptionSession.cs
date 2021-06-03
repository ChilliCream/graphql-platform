using System;
using HotChocolate.Execution.Processing;

namespace HotChocolate.AspNetCore.Subscriptions
{
    /// <summary>
    /// Represents a session with an execution engine subscription.
    /// A subscription session is created within a <see cref="ISocketSession"/>.
    /// Each socket session can have multiple subscription sessions open.
    /// </summary>
    public interface ISubscriptionSession : IDisposable
    {
        /// <summary>
        /// An event that indicates that the underlying subscription has completed.
        /// </summary>
        event EventHandler? Completed;

        /// <summary>
        /// Gets the subscription id that the client has provided.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the underlying subscription.
        /// </summary>
        ISubscription Subscription { get; }
    }
}
