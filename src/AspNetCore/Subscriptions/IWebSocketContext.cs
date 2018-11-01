using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface IWebSocketContext
        : IDisposable
    {
        HttpContext HttpContext { get; }

        QueryExecuter QueryExecuter { get; }

        WebSocketCloseStatus? CloseStatus { get; }

        void RegisterSubscription(ISubscription subscription);

        void UnregisterSubscription(string subscriptionId);

        Task SendMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken);

        Task ReceiveMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken);
    }
}
