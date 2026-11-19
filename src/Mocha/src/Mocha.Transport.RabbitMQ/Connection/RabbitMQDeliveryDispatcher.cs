using System.Buffers;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Runs the deliveries of one consumer on a fixed number of workers, and counts the handlers that run. It
/// outlives the channels the deliveries arrive on.
/// </summary>
/// <param name="maxConcurrency">The number of workers, which is the largest number of running handlers.</param>
/// <param name="messageHandler">Handles one delivered message.</param>
/// <param name="logger">The logger for failed handlers.</param>
/// <param name="queueName">The name of the queue the deliveries come from.</param>
internal sealed class RabbitMQDeliveryDispatcher(
    ushort maxConcurrency,
    Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
    RabbitMQConsumerLogger logger,
    string queueName)
{
    private readonly Channel<Delivery> _deliveries = Channel.CreateUnbounded<Delivery>();
    private readonly object _sync = new();
    private TaskCompletionSource? _waiter;
    private int _running;

    /// <summary>
    /// Starts the workers. Call it one time.
    /// </summary>
    public void Start()
    {
        for (var i = 0; i < maxConcurrency; i++)
        {
            _ = Task.Run(WorkAsync);
        }
    }

    /// <summary>
    /// Gets the number of handlers that have not returned, over all attachments.
    /// </summary>
    public int RunningCount => Volatile.Read(ref _running);

    /// <summary>
    /// Queues a delivery without blocking. Returns <c>false</c> after <see cref="Complete"/>, and the caller
    /// keeps the buffer of the delivery.
    /// </summary>
    public bool TryEnqueue(Delivery delivery) => _deliveries.Writer.TryWrite(delivery);

    /// <summary>
    /// Waits until fewer handlers run than the maximum concurrency.
    /// </summary>
    public Task WaitForCapacityAsync(CancellationToken cancellationToken)
        => WaitUntilAsync(maxConcurrency - 1, cancellationToken);

    /// <summary>
    /// Waits until no handler runs.
    /// </summary>
    public Task WaitForIdleAsync(CancellationToken cancellationToken) => WaitUntilAsync(0, cancellationToken);

    /// <summary>
    /// Discards the deliveries that no worker has taken yet.
    /// </summary>
    public void DiscardQueued()
    {
        while (_deliveries.Reader.TryRead(out var delivery))
        {
            ArrayPool<byte>.Shared.Return(delivery.Buffer);
        }
    }

    /// <summary>
    /// Stops the dispatcher for good. Queued deliveries are discarded, later deliveries are not accepted, and
    /// each worker ends after its current handler.
    /// </summary>
    public void Complete()
    {
        _deliveries.Writer.TryComplete();
        DiscardQueued();
    }

    private async Task WorkAsync()
    {
        await foreach (var delivery in _deliveries.Reader.ReadAllAsync())
        {
            try
            {
                var attachment = delivery.Attachment;
                lock (_sync)
                {
                    // A delivery of a retired attachment does not start. The broker delivers it again.
                    if (attachment.IsRetired)
                    {
                        continue;
                    }

                    _running++;
                }

                try
                {
                    await messageHandler(attachment.Channel, delivery.EventArgs, attachment.HandlerToken);
                }
                catch (OperationCanceledException) when (attachment.HandlerToken.IsCancellationRequested)
                {
                    // The channel was lost or the stop stopped waiting.
                }
                catch (Exception ex)
                {
                    logger.DeliveryFailed(ex, queueName, delivery.EventArgs);
                }
                finally
                {
                    TaskCompletionSource? waiter;
                    lock (_sync)
                    {
                        _running--;
                        waiter = _waiter;
                        _waiter = null;
                    }

                    waiter?.TrySetResult();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(delivery.Buffer);
            }
        }
    }

    private async Task WaitUntilAsync(int maxRunning, CancellationToken cancellationToken)
    {
        while (true)
        {
            Task changed;
            lock (_sync)
            {
                if (_running <= maxRunning)
                {
                    return;
                }

                changed = (
                    _waiter ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
            }

            await changed.WaitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// A delivered message, the attachment it arrived on and the rented buffer that holds its body.
    /// </summary>
    /// <param name="Attachment">The attachment the message arrived on.</param>
    /// <param name="EventArgs">The delivery.</param>
    /// <param name="Buffer">The rented buffer behind the body of the delivery.</param>
    public readonly record struct Delivery(
        RabbitMQConsumerAttachment Attachment,
        BasicDeliverEventArgs EventArgs,
        byte[] Buffer);
}
