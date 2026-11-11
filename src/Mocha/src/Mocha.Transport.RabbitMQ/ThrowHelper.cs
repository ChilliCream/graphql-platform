using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

internal static class ThrowHelper
{
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

    public static Exception BeforeAndAfterConflict()
        => Mocha.ThrowHelper.BeforeAndAfterConflict();

    public static Exception TemporaryEndpointQueueConflict(string queueName, string? endpointName)
        => new InvalidOperationException(
            $"Queue '{queueName}' is explicitly declared without auto-delete, which conflicts with "
            + $"receive endpoint '{endpointName}' being marked Temporary(). Declare the queue with "
            + "AutoDelete(), or remove Temporary() from the endpoint.");

    public static OperationInterruptedException ConnectionClosedDuringSetup(ShutdownEventArgs? reason)
        => reason is null ? new OperationInterruptedException() : new AlreadyClosedException(reason);

    public static TimeoutException ConnectionTimedOut(TimeSpan timeout, Exception innerException)
        => new($"Connecting to RabbitMQ did not complete within {timeout}.", innerException);
}
