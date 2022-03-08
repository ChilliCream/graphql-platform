using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.VisualBasic;
using static HotChocolate.Utilities.Transport.Sockets.Constants;

namespace HotChocolate.Utilities.Transport.Sockets;

public interface IClientSocketSession : IAsyncDisposable
{
    WebSocket Socket { get; }

    ValueTask InitializeAsync<T>(T payload, CancellationToken cancellationToken = default);

    IAsyncEnumerable<object> ExecuteAsync(OperationRequest request);
}

internal class Test : IClientSocketSession
{
    private readonly CancellationTokenSource _cts = new();
    private readonly MessageReceiver _receiver;
    private readonly TestMessageProcessor _processor;
    private bool _disposed;

    public Test(WebSocket socket)
    {
        Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        var pipe = new Pipe();
        _receiver = new MessageReceiver(socket, pipe.Writer);
        _processor = new TestMessageProcessor(pipe.Reader);

        _receiver.Begin(_cts.Token);
        _processor.Begin(_cts.Token);
    }

    public WebSocket Socket { get; }

    public async ValueTask InitializeAsync<T>(
        T payload,
        CancellationToken cancellationToken = default)
    {
        try
        {

        }

        var initPromise = new TaskCompletionSource<bool>();




        await initPromise.Task.ConfigureAwait(false);
    }

    public IAsyncEnumerable<object> ExecuteAsync(OperationRequest request)
    {
        throw new NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();

            await Socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "User closed connection.",
                CancellationToken.None)
                .ConfigureAwait(false);

            _disposed = true;
        }
    }
}

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
            TaskCreationOptions.LongRunning,
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
                    memory.Span[0] = Delimiter;
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

internal sealed class TestMessageProcessor : MessageProcessor
{
    public TestMessageProcessor(PipeReader reader) : base(reader) { }

    protected override ValueTask ProcessMessageAsync(
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal abstract class MessageProcessor : IObservable<OperationMessage>
{
    private readonly PipeReader _reader;
    private readonly ObserverHolder _observerHolder = new();

    protected MessageProcessor(PipeReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public void Begin(CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => ProcessMessagesAsync(cancellationToken),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                SequencePosition? position;
                var result = await _reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                do
                {
                    position = buffer.PositionOf(Delimiter);

                    if (position is not null)
                    {
                        await ProcessMessageAsync(
                            buffer.Slice(0, position.Value),
                            cancellationToken)
                            .ConfigureAwait(false);

                        // Skip the message which was read.
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while (position != null);

                _reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // we will just stop receiving
        }
        catch (WebSocketException)
        {
            // we will just stop receiving
        }
        finally
        {
            // reader should be completed always, so that related pipe writer can
            // stop write new messages
            await _reader.CompleteAsync();
        }
    }

    protected abstract ValueTask ProcessMessageAsync(
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken);

    public IDisposable Subscribe(IObserver<OperationMessage> observer)
    {
        if (_observerHolder.Observer is null)
        {
            throw new InvalidOperationException("There can only be one observer.");
        }

        _observerHolder.Observer = observer;
        return _observerHolder;
    }

    private sealed class ObserverHolder : IDisposable
    {
        public IObserver<OperationMessage>? Observer { get; set; }

        public void Dispose()
        {
            Observer = null;
        }
    }
}

internal static class Constants
{
    internal const byte Delimiter = 0x07;
}


internal abstract class OperationMessage
{
    public string Type { get; }
}
