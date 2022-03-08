using System.IO.Pipelines;
using System.Net.WebSockets;
using HotChocolate.Utilities.Transport.Sockets.Helpers;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols;

internal sealed class MessageReceiver
{
    private const int _maxMessageSize = 512;
    private readonly WebSocket _socket;
    private readonly PipeWriter _writer;

    public MessageReceiver(WebSocket socket, PipeWriter writer)
    {
        _socket = socket;
        _writer = writer;
    }

    public void Begin(CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => ReceiveMessagesAsync(cancellationToken),
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.Default);

    private async ValueTask ReceiveMessagesAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested || _socket is not { State: WebSocketState.Open })
        {
            return;
        }

        try
        {
            ValueWebSocketReceiveResult socketResult;
            do
            {
                if (_socket.State is not WebSocketState.Open)
                {
                    break;
                }

                Memory<byte> memory = _writer.GetMemory(_maxMessageSize);
                socketResult = await _socket.ReceiveAsync(memory, ct).ConfigureAwait(false);;
                _writer.Advance(socketResult.Count);

                if (socketResult.EndOfMessage)
                {
                    memory = _writer.GetMemory(1);
                    memory.Span[0] = Constants.Delimiter;
                    _writer.Advance(1);
                    await _writer.FlushAsync(ct).ConfigureAwait(false);
                    break;
                }
            } while (!socketResult.EndOfMessage);
        }
        catch
        {
            // swallow exception, there's nothing we can reasonably do.
        }
    }
}
