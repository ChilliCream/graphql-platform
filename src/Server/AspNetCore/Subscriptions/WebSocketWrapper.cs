#if !ASPNETCLASSIC
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class WebSocketWrapper : IWebSocket
    {
        private const int _maxMessageSize = 1024 * 4;

        private readonly WebSocket _webSocket;
        private bool _disposed;

        public WebSocketWrapper(
            WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public bool Closed => _webSocket.CloseStatus.HasValue;

        public async Task SendAsync(
            Stream messageStream,
            CancellationToken cancellationToken)
        {
            var read = 0;
            var buffer = new byte[_maxMessageSize];

            do
            {
                read = messageStream.Read(buffer, 0, buffer.Length);
                var segment = new ArraySegment<byte>(buffer, 0, read);
                var isEOF = messageStream.Position == messageStream.Length;

                await _webSocket.SendAsync(
                        segment, WebSocketMessageType.Text,
                        isEOF, cancellationToken)
                    .ConfigureAwait(false);
            } while (read == _maxMessageSize);
        }

        public async Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            WebSocketReceiveResult socketResult = null;
            do
            {
                Memory<byte> memory = writer.GetMemory(_maxMessageSize);
                bool success = MemoryMarshal
                    .TryGetArray(memory, out ArraySegment<byte> buffer);
                if (success)
                {
                    try
                    {
                        socketResult = await _webSocket
                            .ReceiveAsync(buffer, cancellationToken)
                            .ConfigureAwait(false);

                        if (socketResult.Count == 0)
                        {
                            break;
                        }

                        writer.Advance(socketResult.Count);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    FlushResult result = await writer
                        .FlushAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            } while (socketResult == null || !socketResult.EndOfMessage);
        }

        public async Task CloseAsync(
            string message,
            CancellationToken cancellationToken)
        {
            if (_webSocket.CloseStatus.HasValue)
            {
                return;
            }

            await _webSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    message,
                    cancellationToken)
                .ConfigureAwait(false);

            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _webSocket?.Dispose();
            }
        }
    }
}

#endif
