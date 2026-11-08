using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

internal static class ThrowHelper
{
    public static InvalidOperationException ReplyEndpointNotStarted()
        => new("The RabbitMQ reply endpoint must be started before dispatching requests.");

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

    public static TimeoutException ConnectionTimedOut(TimeSpan timeout, Exception innerException)
        => new($"Connecting to RabbitMQ did not complete within {timeout}.", innerException);

    public static InvalidOperationException MessageReturned(string queueName, Exception innerException)
        => new(
            $"The broker returned the message because no queue named '{queueName}' exists.",
            innerException);
}
