using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Identifies whether a failed RabbitMQ operation can succeed on a later attempt.
/// </summary>
internal enum RabbitMQFailureKind
{
    /// <summary>
    /// The connection or the broker is unavailable, and a later attempt can succeed.
    /// </summary>
    Transient,

    /// <summary>
    /// The broker or the configuration refuses the operation, and it fails the same way on every attempt.
    /// </summary>
    Permanent
}

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

                case AuthenticationFailureException or AuthenticationException or PublishException:
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
