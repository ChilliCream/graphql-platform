#if !ASPNETCLASSIC

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal interface IWebSocketContext
        : IDisposable
    {
        HttpContext HttpContext { get; }

        IQueryExecutor QueryExecutor { get; }

        WebSocketCloseStatus? CloseStatus { get; }

        IDictionary<string, object> RequestProperties { get; }

        void RegisterSubscription(ISubscription subscription);

        void UnregisterSubscription(string subscriptionId);

        Task PrepareRequestAsync(QueryRequest request);

        Task SendMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken);

        Task ReceiveMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken);

        Task<ConnectionStatus> OpenAsync(
            IDictionary<string, object> properties);

        Task CloseAsync();
    }
}

#endif
