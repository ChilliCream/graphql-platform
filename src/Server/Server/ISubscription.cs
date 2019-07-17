using System;

namespace HotChocolate.Server
{
    public interface ISubscription
        : IDisposable
    {
        event EventHandler Completed;

        string Id { get; }
    }
}
