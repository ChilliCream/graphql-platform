using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Classifies failures of RabbitMQ operations for connection startup, recovery logging and publish retries.
/// </summary>
internal static class RabbitMQFailure
{
    /// <summary>
    /// Gets whether a later attempt of the failed operation can succeed. Callers handle their own
    /// cancellation before they ask.
    /// </summary>
    public static RabbitMQFailureKind Classify(Exception exception)
    {
        // A returned message has no route and fails. A rejected message is attempted again. An exception
        // that wraps one of the two has decided what the broker's answer means, and is classified by itself.
        if (exception is PublishException publish)
        {
            return publish.IsReturn ? RabbitMQFailureKind.Permanent : RabbitMQFailureKind.Transient;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                // One entry per broker node. A node that is only unreachable keeps the failure transient.
                case AggregateException { InnerExceptions.Count: > 0 } nodes:
                    foreach (var node in nodes.InnerExceptions)
                    {
                        if (Classify(node) == RabbitMQFailureKind.Transient)
                        {
                            return RabbitMQFailureKind.Transient;
                        }
                    }

                    return RabbitMQFailureKind.Permanent;

                case AuthenticationFailureException or AuthenticationException:
                    return RabbitMQFailureKind.Permanent;

                // The broker closed the channel or the connection and said why.
                case OperationInterruptedException { ShutdownReason: { Initiator: ShutdownInitiator.Peer } reason }:
                    return reason.ReplyCode is Constants.ConnectionForced or Constants.InternalError
                        ? RabbitMQFailureKind.Transient
                        : RabbitMQFailureKind.Permanent;
            }
        }

        return exception
            is IOException
                or SocketException
                or TimeoutException
                or OperationCanceledException
                or ObjectDisposedException
                or ChannelClosedException
                or OperationInterruptedException
                or ConnectFailureException
            ? RabbitMQFailureKind.Transient
            : RabbitMQFailureKind.Permanent;
    }

    /// <summary>
    /// Gets one line that names a failure in a log message: the reason the channel or the connection was
    /// closed with when the failure has one, otherwise the type and the message of its innermost exception.
    /// </summary>
    public static string Describe(Exception exception)
    {
        var innermost = exception;
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationInterruptedException { ShutdownReason: { } reason })
            {
                return $"{reason.ReplyCode} {reason.ReplyText} (initiator {reason.Initiator})";
            }

            innermost = current;
        }

        return $"{innermost.GetType().Name}: {innermost.Message.TrimEnd('.')}";
    }

    /// <summary>
    /// Gets whether the failed operation fails the same way on every attempt.
    /// </summary>
    public static bool IsPermanent(Exception exception) => Classify(exception) == RabbitMQFailureKind.Permanent;

    /// <summary>
    /// Gets the level a failed attempt that is retried is logged at: <see cref="LogLevel.Warning"/> for a
    /// transient failure, and <see cref="LogLevel.Error"/> for a permanent one.
    /// </summary>
    public static LogLevel GetLogLevel(Exception exception)
        => IsPermanent(exception) ? LogLevel.Error : LogLevel.Warning;
}
