using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Classifies failures of RabbitMQ operations for connection startup, recovery logging and publish retries.
/// </summary>
internal static class RabbitMQFailure
{
    /// <summary>
    /// Gets whether the broker refused the operation, which fails the same way on every attempt: refused
    /// credentials, refused virtual host access, or a rejected operation such as a declare with different arguments.
    /// </summary>
    public static bool IsBrokerRejection(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AuthenticationFailureException || IsRejectedOperation(current))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the level a failed recovery attempt is logged at: <see cref="LogLevel.Warning"/> for failures a
    /// later attempt can recover from, and <see cref="LogLevel.Error"/> for all others.
    /// </summary>
    public static LogLevel GetLogLevel(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Count > 0
                && aggregate.InnerExceptions.All(static ex => GetLogLevel(ex) == LogLevel.Warning)
                    ? LogLevel.Warning
                    : LogLevel.Error;
        }

        if (exception.InnerException is { } inner && GetLogLevel(inner) == LogLevel.Error)
        {
            return LogLevel.Error;
        }

        return exception switch
        {
            AuthenticationException or PossibleAuthenticationFailureException => LogLevel.Error,
            _ when IsRejectedOperation(exception) => LogLevel.Error,
            IOException or SocketException or TimeoutException or OperationCanceledException
                or ObjectDisposedException or BrokerUnreachableException or ConnectFailureException
                or OperationInterruptedException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }

    /// <summary>
    /// Gets whether a publish failed because its connection was lost, so that publishing again can succeed.
    /// Returns <c>false</c> once the caller cancelled the publish or the dispatcher was disposed.
    /// </summary>
    public static bool IsConnectionLoss(
        Exception exception,
        RabbitMQDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || dispatcher.IsShutDown)
        {
            return false;
        }

        return exception switch
        {
            // The broker rejected the message, and publishing it again fails the same way.
            PublishException => false,

            // A cancellation the caller did not request comes from a timed out operation on a broken connection.
            OperationCanceledException or ObjectDisposedException => true,
            TimeoutException => !dispatcher.IsConnected,
            BrokerUnreachableException { InnerException: AuthenticationFailureException } => false,
            BrokerUnreachableException or ConnectFailureException => true,
            OperationInterruptedException { ShutdownReason: var reason } => IsConnectionLoss(reason),
            IOException or SocketException => true,
            _ => false
        };
    }

    private static bool IsConnectionLoss(ShutdownEventArgs? reason)
        => reason is null
            || reason.Initiator != ShutdownInitiator.Peer
            || reason.ReplyCode
                is Constants.ConnectionForced
                or Constants.FrameError
                or Constants.SyntaxError
                or Constants.CommandInvalid
                or Constants.ChannelError
                or Constants.UnexpectedFrame
                or Constants.ResourceError
                or Constants.NotImplemented
                or Constants.InternalError;

    private static bool IsRejectedOperation(Exception exception)
        => exception is OperationInterruptedException
        {
            ShutdownReason.ReplyCode:
                Constants.AccessRefused
                or Constants.NotFound
                or Constants.ResourceLocked
                or Constants.PreconditionFailed
                or Constants.NotAllowed
        };
}
