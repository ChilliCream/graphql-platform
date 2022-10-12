using System.Threading.Channels;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Nats;

/// <summary>
/// Represents the NATS event stream.
/// </summary>
/// <typeparam name="TMessage">
/// The message type.
/// </typeparam>
public sealed class NatsEventStream<TMessage> : ISourceStream<TMessage>
{
    private readonly Channel<TMessage> _channel;
    private readonly IDisposable _subscription;

    /// <summary>
    /// Initializes a new instance of <see cref="TMessage"/>.
    /// </summary>
    /// <param name="channel">
    /// The internal message channel.
    /// </param>
    /// <param name="subscription">
    /// The subscription.
    /// </param>
    public NatsEventStream(Channel<TMessage> channel, IDisposable subscription)
    {
        _channel = channel;
        _subscription = subscription;
    }

    /// <inheritdoc />
    IAsyncEnumerable<TMessage> ISourceStream<TMessage>.ReadEventsAsync()
        => new NatsAsyncEnumerable(_channel.Reader);

    /// <inheritdoc />
    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
        => (IAsyncEnumerable<object>)new NatsAsyncEnumerable(_channel.Reader);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _subscription.Dispose();
        return ValueTask.CompletedTask;
    }

    private class NatsAsyncEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<TMessage> _reader;

        public NatsAsyncEnumerable(ChannelReader<TMessage> reader)
        {
            _reader = reader;
        }

        public async IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var message))
                {
                    yield return message!;
                }
            }
        }
    }
}
