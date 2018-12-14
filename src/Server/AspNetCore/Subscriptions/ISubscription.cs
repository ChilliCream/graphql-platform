#if !ASPNETCLASSIC

using System;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal interface ISubscription
        : IDisposable
    {
        event EventHandler Completed;

        string Id { get; }
    }
}

#endif
