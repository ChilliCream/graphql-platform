using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SocketConnectionMock : ISocketConnection
    {
        public SocketConnectionMock()
        {
            Subscriptions = new SubscriptionManager(this);
        }

        public HttpContext HttpContext { get; }

        public bool Closed { get; set; }

        public ISubscriptionManager Subscriptions { get; set; }

        public IServiceProvider RequestServices { get; set; }

        public CancellationToken RequestAborted { get; }

        public List<byte[]> SentMessages { get; } = new List<byte[]>();

        public Task<bool> TryOpenAsync()
        {
            return Task.FromResult(true);
        }

        public Task CloseAsync(
            string message,
            SocketCloseStatus closeStatus,
            CancellationToken cancellationToken)
        {
            Closed = true;
            return Task.CompletedTask;
        }

        public Task SendAsync(
            byte[] message,
            CancellationToken cancellationToken)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }
    }
}
