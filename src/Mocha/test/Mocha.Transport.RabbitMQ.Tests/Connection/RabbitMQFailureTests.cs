using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQFailureTests
{
    [Theory]
    [MemberData(nameof(TransientFailures))]
    public void Classify_Should_ReturnTransient_When_LaterAttemptCanSucceed(string shape, Exception exception)
    {
        // act
        var kind = RabbitMQFailure.Classify(exception);

        // assert
        Assert.True(kind == RabbitMQFailureKind.Transient, shape);
    }

    [Theory]
    [MemberData(nameof(PermanentFailures))]
    public void Classify_Should_ReturnPermanent_When_EveryAttemptFailsTheSameWay(string shape, Exception exception)
    {
        // act
        var kind = RabbitMQFailure.Classify(exception);

        // assert
        Assert.True(kind == RabbitMQFailureKind.Permanent, shape);
    }

    [Fact]
    public void GetLogLevel_Should_ReturnWarning_When_FailureIsTransient()
    {
        // arrange
        var exception = new AlreadyClosedException(Shutdown(ShutdownInitiator.Library, 0, "End of stream"));

        // act
        var level = RabbitMQFailure.GetLogLevel(exception);

        // assert
        Assert.Equal(LogLevel.Warning, level);
    }

    [Fact]
    public void GetLogLevel_Should_ReturnError_When_FailureIsPermanent()
    {
        // arrange
        var exception = new BrokerUnreachableException(new AuthenticationFailureException("ACCESS_REFUSED"));

        // act
        var level = RabbitMQFailure.GetLogLevel(exception);

        // assert
        Assert.Equal(LogLevel.Error, level);
    }

    [Theory]
    [MemberData(nameof(FailuresWithCloseReason))]
    public void Describe_Should_ReturnCloseReason_When_ChainHoldsOne(string expected, Exception exception)
    {
        // act
        var description = RabbitMQFailure.Describe(exception);

        // assert
        Assert.Equal(expected, description);
    }

    [Theory]
    [MemberData(nameof(FailuresWithoutCloseReason))]
    public void Describe_Should_ReturnTypeAndMessageOfInnermostException_When_ChainHoldsNoCloseReason(
        string expected,
        Exception exception)
    {
        // act
        var description = RabbitMQFailure.Describe(exception);

        // assert
        Assert.Equal(expected, description);
    }

    public static TheoryData<string, Exception> FailuresWithCloseReason()
        => new()
        {
            {
                "0 End of stream (initiator Library)",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Library, 0, "End of stream"))
            },
            {
                "0 End of stream (initiator Library)",
                new AlreadyClosedException(
                    new ShutdownEventArgs(
                        ShutdownInitiator.Library,
                        0,
                        "End of stream",
                        new IOException("Unable to read data from the transport connection.")))
            },
            {
                "404 NOT_FOUND - no exchange 'orders' in vhost '/' (initiator Peer)",
                new TimeoutException(
                    "The broker did not answer a request of the connection attempt in time.",
                    new OperationInterruptedException(
                        Shutdown(ShutdownInitiator.Peer, Constants.NotFound, "NOT_FOUND - no exchange 'orders' in vhost '/'")))
            },
            {
                "320 CONNECTION_FORCED - broker forced connection closure (initiator Peer)",
                new BrokerUnreachableException(
                    new PossibleAuthenticationFailureException(
                        "Possibly caused by authentication failure",
                        new OperationInterruptedException(
                            Shutdown(
                                ShutdownInitiator.Peer,
                                Constants.ConnectionForced,
                                "CONNECTION_FORCED - broker forced connection closure"))))
            }
        };

    public static TheoryData<string, Exception> FailuresWithoutCloseReason()
        => new()
        {
            { "PublishException: Message rejected by broker", new PublishException(1, isReturn: false) },
            {
                "OperationInterruptedException: The AMQP operation was interrupted",
                new OperationInterruptedException()
            },
            {
                "TaskCanceledException: The request was cancelled",
                new TimeoutException(
                    "The broker did not answer the request to open a channel.",
                    new TaskCanceledException("The request was cancelled."))
            },
            {
                "IOException: Unable to read data from the transport connection",
                new BrokerUnreachableException(
                    new AggregateException(
                        new ConnectFailureException(
                            "Connection failed",
                            new IOException("Unable to read data from the transport connection."))))
            },
            { "TimeoutException: The operation has timed out", new TimeoutException("The operation has timed out") }
        };

    // Every entry is built to an exception chain that RabbitMQ.Client 7.2.2 produced against a broker.
    public static TheoryData<string, Exception> TransientFailures()
        => new()
        {
            {
                "the broker forced the connection closed (320)",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Peer, Constants.ConnectionForced, "CONNECTION_FORCED"))
            },
            {
                "the broker reported an internal error (541)",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Peer, Constants.InternalError, "INTERNAL_ERROR"))
            },
            {
                "the socket ended or a heartbeat was missed",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Library, 0, "End of stream"))
            },
            {
                "the application closed the connection",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Application, 200, "Goodbye"))
            },
            { "an operation was interrupted without a reason", new OperationInterruptedException() },
            {
                "no node could be reached",
                new BrokerUnreachableException(
                    new AggregateException(
                        new ConnectFailureException("Connection failed", new SocketException(111))))
            },
            {
                "one node refused TLS and another one could not be reached",
                new BrokerUnreachableException(
                    new AggregateException(
                        new AuthenticationException("The remote certificate is invalid"),
                        new ConnectFailureException("Connection failed", new SocketException(111))))
            },
            {
                "the connection dropped during the handshake",
                new BrokerUnreachableException(
                    new PossibleAuthenticationFailureException(
                        "Possibly caused by authentication failure",
                        new OperationInterruptedException(Shutdown(ShutdownInitiator.Library, 0, "End of stream"))))
            },
            { "an operation timed out", new TimeoutException("Connecting to RabbitMQ did not complete") },
            { "the client timed out a request", new TaskCanceledException() },
            { "a channel or connection was disposed", new ObjectDisposedException("channel") },
            { "the frame writer of the client closed", new ChannelClosedException() },
            { "the socket failed", new IOException("Unable to write data to the transport connection") },
            { "the socket was reset", new SocketException(104) },
            { "the broker rejected the message", new PublishException(1, isReturn: false) }
        };

    public static TheoryData<string, Exception> PermanentFailures()
        => new()
        {
            {
                "the credentials were refused",
                new BrokerUnreachableException(new AuthenticationFailureException("ACCESS_REFUSED"))
            },
            {
                "the virtual host was refused (530)",
                new OperationInterruptedException(Shutdown(ShutdownInitiator.Peer, Constants.NotAllowed, "NOT_ALLOWED"))
            },
            {
                "a declaration did not match (406)",
                new AlreadyClosedException(
                    Shutdown(ShutdownInitiator.Peer, Constants.PreconditionFailed, "PRECONDITION_FAILED"))
            },
            {
                "a queue or exchange does not exist (404)",
                new OperationInterruptedException(Shutdown(ShutdownInitiator.Peer, Constants.NotFound, "NOT_FOUND"))
            },
            {
                "a queue is locked by another connection (405)",
                new OperationInterruptedException(
                    Shutdown(ShutdownInitiator.Peer, Constants.ResourceLocked, "RESOURCE_LOCKED"))
            },
            {
                "access to a resource was refused (403)",
                new OperationInterruptedException(
                    Shutdown(ShutdownInitiator.Peer, Constants.AccessRefused, "ACCESS_REFUSED"))
            },
            {
                "a frame was larger than the broker accepts (501)",
                new AlreadyClosedException(Shutdown(ShutdownInitiator.Peer, Constants.FrameError, "FRAME_ERROR"))
            },
            {
                "every node refused TLS",
                new BrokerUnreachableException(
                    new AggregateException(new AuthenticationException("The remote certificate is invalid")))
            },
            { "the broker returned a message without a queue to repair", new PublishException(1, isReturn: true) },
            {
                "the broker returned the message",
                new InvalidOperationException(
                    "The broker returned the message because no queue named 'orders' exists.",
                    new PublishException(1, isReturn: true))
            },
            {
                "the broker rejected a message that is not published again",
                new InvalidOperationException(
                    "The broker rejected the message that was published to exchange 'orders'.",
                    new PublishException(1, isReturn: false))
            },
            { "the connection has no free channel", new ChannelAllocationException() },
            { "the failure is unknown", new InvalidOperationException("Something else") }
        };

    private static ShutdownEventArgs Shutdown(ShutdownInitiator initiator, ushort replyCode, string replyText)
        => new(initiator, replyCode, replyText);
}
