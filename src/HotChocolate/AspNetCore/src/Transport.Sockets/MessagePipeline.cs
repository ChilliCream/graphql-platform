using System.IO.Pipelines;

namespace HotChocolate.Transport.Sockets;

/// <summary>
/// Incoming message pipeline.
/// </summary>
public sealed class MessagePipeline
{
    private readonly MessageReceiver _messageReceiver;
    private readonly MessageProcessor _messageProcessor;

    public EventHandler? Completed;

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
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        if (messageHandler is null)
        {
            throw new ArgumentNullException(nameof(messageHandler));
        }

        var pipe = new Pipe();
        _messageReceiver = new MessageReceiver(socket, pipe.Writer);
        _messageProcessor = new MessageProcessor(messageHandler, pipe.Reader);
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
        Completed?.Invoke(this, EventArgs.Empty);
    }
}
