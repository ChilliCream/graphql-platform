using System.IO.Pipelines;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Subscribes to a <see cref="ISocketClient"/> to receive data and pipes the received messages
/// separated by a delimiter into a pipe.
/// </summary>
internal static class MessageReceiver
{
    /// <summary>
    /// Subscribes to a <see cref="ISocketClient"/> to receive data and pipes the received
    /// messages separated by a delimiter into a pipe.
    /// </summary>
    /// <param name="writer">The writer the message to write to</param>
    /// <param name="client">The client to subscribe to</param>
    /// <param name="cancellationToken">The cancellation token that stops the receiving</param>
    /// <returns>
    /// A tasks that stops when <paramref name="cancellationToken"/> is cancelled
    /// </returns>
    public static Task Start(
        PipeWriter writer,
        ISocketClient client,
        CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => ReceiveAsync(client, writer, cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private static async Task ReceiveAsync(
        ISocketClient client,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!client.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                await client
                    .ReceiveAsync(writer, cancellationToken)
                    .ConfigureAwait(false);

                await writer.WriteMessageDelimiterAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // if this operation was cancelled we will move on.
        }
        finally
        {
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    private static async Task WriteMessageDelimiterAsync(
        this PipeWriter writer,
        CancellationToken cancellationToken)
    {
        var memory = writer.GetMemory(1);
        memory.Span[0] = MessageProcessor.Delimiter;
        writer.Advance(1);

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
