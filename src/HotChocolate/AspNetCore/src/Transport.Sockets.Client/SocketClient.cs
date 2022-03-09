using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client;

public class SocketClient : ISocketClient, ISocket
{
    private const int _maxMessageSize = 512;
    private readonly WebSocket _socket;
    private bool _disposed;
    public bool IsClosed { get; }

    public static SocketClient From(WebSocket socket)
    {
        throw new NotImplementedException();
    }

    public ValueTask InitializeAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ResultStream> ExecuteAsync(OperationRequest request)
    {
        throw new NotImplementedException();
    }

    async Task<bool> ISocket.ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken)
    {
        if (_disposed || _socket is not { State: WebSocketState.Open })
        {
            return false;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(_maxMessageSize);
        var arraySegment = new ArraySegment<byte>(buffer);

        try
        {
            var read = 0;
            WebSocketReceiveResult socketResult;

            do
            {
                if (_socket.State is not WebSocketState.Open)
                {
                    break;
                }

                // read message segment from socket.
                socketResult = await _socket.ReceiveAsync(arraySegment, cancellationToken);

                // copy message segment to writer.
                Memory<byte> memory = writer.GetMemory(socketResult.Count);
                buffer.AsSpan().Slice(0, socketResult.Count).CopyTo(memory.Span);
                writer.Advance(socketResult.Count);
                read += socketResult.Count;
            } while (!socketResult.EndOfMessage);

            return read > 0;
        }
        catch
        {
            // swallow exception, there's nothing we can reasonably do.
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}

public sealed class ResultStream
{
    public bool IsSuccessResult => Errors.Count is 0;

    public IReadOnlyList<JsonElement> Errors { get; }

    public IAsyncEnumerable<OperationResult> ReadResultsAsync()
    {
        throw new NotImplementedException();
    }
}
