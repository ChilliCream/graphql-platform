using System;
using System.Net.WebSockets;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface IWebSocketContext
        : IDisposable
    {
        HttpContext HttpContext { get; }

        QueryExecuter QueryExecuter { get; }

        WebSocket WebSocket { get; }

        void RegisterSubscription(ISubscription subscription);

        void UnregisterSubscription(string subscriptionId);
    }
}
