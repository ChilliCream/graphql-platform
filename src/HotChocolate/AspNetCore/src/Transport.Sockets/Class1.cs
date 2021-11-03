using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace Transport.Sockets;

public interface ISocketConnection
{
    ValueTask SendAsync(ReadOnlySpan<byte> message, CancellationToken cancellationToken);

    // ValueTask OnReceiveAsync(ReadOnlySpan<byte> message, CancellationToken cancellationToken);

    Task CloseAsync(
        string message,
        SocketCloseStatus closeStatus,
        CancellationToken cancellationToken);
}

public enum SocketCloseStatus
{
    NormalClosure,
    InternalServerError
}

public interface IMessageProtocol
{
    IMessageSender Sender { get; }

    IMessageReceiver Receiver { get; }
}

public interface IMessageReceiver
{
    ValueTask OnReceiveAsync(
        ISocketSession session,
        IMessage message,
        CancellationToken cancellationToken);
}

public interface IMessageSender
{
    ValueTask SendAsync(
        ISocketSession session,
        IMessage message,
        CancellationToken cancellationToken);
}

public interface ISocketSession : IAsyncDisposable
{
    ISocketConnection Connection { get; }

    IMessageProtocol Protocol { get; }

    IRequestExecutor Executor { get; }

    bool IsInitialized { get; set; }

    IStreamOperation RegisterOperation(IResponseStream stream);

    void TryCompleteOperation(string id);

    ValueTask SendAsync(IMessage message, CancellationToken cancellationToken);
}

public interface IStreamOperation : IDisposable
{
    string Id { get; }

    public ISocketSession Session { get; }

    public IResponseStream Stream { get; }

    public CancellationToken RequestAborted { get; }
}

public interface IMessage
{
    string Type { get; }
}



