using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions;

public class SocketConnectionMock : ISocketConnection
{
    public SocketConnectionMock()
    {
        Operations = new OperationManager();
    }

    public HttpContext HttpContext => default!;

    public bool IsClosed { get; set; }

    public IOperationManager Operations { get; }

    public IServiceProvider RequestServices { get; set; } = default!;

    public CancellationToken RequestAborted => default;

    public List<byte[]> SentMessages { get; } = new();

    public Task<IProtocolHandler> TryAcceptConnection()
        => throw new NotImplementedException();

    public Task CloseAsync(
        string message,
        ConnectionCloseReason reason,
        CancellationToken cancellationToken)
    {
        IsClosed = true;
        return Task.CompletedTask;
    }

    public Task SendAsync(ArraySegment<byte> message, CancellationToken cancellationToken)
    {
        SentMessages.Add(message.ToArray());
        return Task.CompletedTask;
    }

    public Task ReceiveAsync(IBufferWriter<byte> writer, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public void Dispose() { }
}
