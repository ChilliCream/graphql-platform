using System;

namespace HotChocolate.AspNetCore.Subscriptions
{
    /// <summary>
    /// Represents a 
    /// </summary>
    public interface ISubscription : IDisposable
    {
        event EventHandler? Completed;

        string Id { get; }
    }
}
