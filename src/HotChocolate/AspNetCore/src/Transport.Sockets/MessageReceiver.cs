using System.Buffers;
using System.IO.Pipelines;
using static HotChocolate.Transport.Sockets.Delimiter;

namespace HotChocolate.Transport.Sockets;

internal sealed class MessageReceiver
{
    private readonly ISocket _listener;
    private readonly PipeWriter _writer;

    public MessageReceiver(ISocket listener, PipeWriter writer)
    {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!_listener.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                if (await _listener.ReadMessageAsync(_writer, cancellationToken))
                {
                    WriteEndOfMessage(_writer);
                    await _writer.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // if the connection was cancelled we will swallow the exception and move on.
        }
        finally
        {
            // writer should be always completed
            await _writer.CompleteAsync();
        }

        static void WriteEndOfMessage(IBufferWriter<byte> writer)
        {
            const int length = 1;
            var span = writer.GetSpan(length);
            span[0] = EndOfText;
            writer.Advance(length);
        }
    }
}
