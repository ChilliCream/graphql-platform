using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transport.Sockets;

public interface ISocketConnection
{
    ValueTask SendAsync(ReadOnlySpan<byte> message, CancellationToken cancellationToken);

    ValueTask OnReceiveAsync(ReadOnlySpan<byte> message, CancellationToken cancellationToken);

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

public interface ISocketSession
{
    IMessageProtocol Protocol { get; }

    IDictionary<string, object?> ContextData { get; }

    bool IsInitialized { get; set; }
}

public interface IMessage
{
    string Type { get; }
}



