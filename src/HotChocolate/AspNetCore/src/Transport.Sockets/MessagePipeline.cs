using System.Collections.Immutable;
using System.IO.Pipelines;

namespace HotChocolate.Transport.Sockets;

/// <summary>
/// Incoming message pipeline.
/// </summary>
public sealed class MessagePipeline
{
    private readonly MessageReceiver _messageReceiver;
    private readonly MessageProcessor _messageProcessor;
    private ImmutableArray<(Action<object?> action, object state)> _subscribers = [];

    /// <summary>
    /// Initializes a new instance of <see cref="MessagePipeline"/>.
    /// </summary>
    /// <param name="socket">
    /// The socket to read messages from.
    /// </param>
    /// <param name="messageHandler">
    /// The message handler to process the messages.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="socket"/> is <c>null</c> or
    /// <paramref name="messageHandler"/> is <c>null</c>.
    /// </exception>
    public MessagePipeline(ISocket socket, IMessageHandler messageHandler)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(messageHandler);

        var pipe = new Pipe();
        _messageReceiver = new MessageReceiver(socket, pipe.Writer);
        _messageProcessor = new MessageProcessor(messageHandler, pipe.Reader);
    }

    /// <summary>
    /// Registers a delegate that will be invoked once the message pipeline has completed.
    /// </summary>
    /// <param name="completed">
    /// The delegate that will be invoked on completion.
    /// </param>
    /// <param name="state">
    /// The state that is passed along.
    /// </param>
    /// <typeparam name="T">
    /// The type of the state.
    /// </typeparam>
    public void OnCompleted<T>(Action<T> completed, T state) where T : class
    {
        _subscribers = _subscribers.Add((o => completed((T)o!), state));
    }

    /// <summary>
    /// Run the pipeline and process messages.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            _messageReceiver.ReceiveMessagesAsync(cancellationToken),
            _messageProcessor.ProcessMessagesAsync(cancellationToken));

        foreach (var (action, state) in _subscribers)
        {
            action(state);
        }
    }
}
