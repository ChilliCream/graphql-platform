using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

internal static class ThrowHelper
{
    public static InvalidOperationException EnvelopeRequired()
        => new("Envelope is not set");

    public static TimeoutException ConnectionAttemptTimedOut(TimeSpan timeout, Exception innerException)
        => new($"The connection attempt did not complete within {timeout}.", innerException);

    public static TimeoutException ConnectionRequestTimedOut(Exception innerException)
        => new("The broker did not answer a request of the connection attempt in time.", innerException);

    public static ChannelOpenBlockedException ChannelOpenBlocked(Exception innerException)
        => new(
            "The broker did not answer the request to open a channel while it blocked the connection.",
            innerException);

    public static TimeoutException ChannelOpenTimedOut(Exception innerException)
        => new("The broker did not answer the request to open a channel.", innerException);

    public static ObjectDisposedException ConnectionManagerDisposed()
        => new(typeof(RabbitMQConnectionManager).FullName);

    public static ObjectDisposedException DispatcherDisposed()
        => new(typeof(RabbitMQDispatcher).FullName);

    public static InvalidOperationException MessageHeadersTooLarge(int size, int reserve, uint frameMax)
        => new(
            $"The headers of the message need {size} bytes. The connection allows frames of at most "
            + $"{frameMax} bytes, of which {reserve} bytes are kept for what the client adds. "
            + "The message was not sent.");

    public static Exception BeforeAndAfterConflict()
        => Mocha.ThrowHelper.BeforeAndAfterConflict();

    public static Exception TemporaryEndpointQueueConflict(string queueName, string? endpointName)
        => new InvalidOperationException(
            $"Queue '{queueName}' is explicitly declared without auto-delete, which conflicts with "
                + $"receive endpoint '{endpointName}' being marked Temporary(). Declare the queue with "
                + "AutoDelete(), or remove Temporary() from the endpoint.");

    public static OperationInterruptedException ConnectionClosedDuringSetup(ShutdownEventArgs? reason)
        => reason is null ? new OperationInterruptedException() : new AlreadyClosedException(reason);

    public static TimeoutException ConsumerAttachTimedOut(string queueName, TimeSpan timeout)
        => new($"Attaching the consumer for queue '{queueName}' did not complete within {timeout}.");

    public static TimeoutException ConsumerAttachRequestTimedOut(string queueName, Exception innerException)
        => new(
            $"The broker did not answer a request while the consumer for queue '{queueName}' attached.",
            innerException);

    public static TimeoutException ConnectionTimedOut(TimeSpan timeout, Exception innerException)
        => new($"Connecting to RabbitMQ did not complete within {timeout}.", innerException);

    public static InvalidOperationException MessageReturned(string queueName, Exception innerException)
        => new(
            $"The broker returned the message because no queue named '{queueName}' exists.",
            innerException);

    public static TimeoutException QueueDeleteTimedOut(string queueName, TimeSpan timeout, Exception innerException)
        => new($"Deleting queue '{queueName}' did not complete within {timeout}.", innerException);

    public static ArgumentOutOfRangeException StartTimeoutOutOfRange(TimeSpan timeout)
        => new(
            nameof(RabbitMQTransportConfiguration.StartTimeout),
            timeout,
            "The start timeout must be between zero and 4,294,967,294 milliseconds, or Timeout.InfiniteTimeSpan.");
}
