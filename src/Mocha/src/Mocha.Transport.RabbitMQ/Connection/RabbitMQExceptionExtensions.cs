using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Gets whether the failed RabbitMQ operation fails the same way on every attempt. Callers handle
        /// their own cancellation before they ask.
        /// </summary>
        public bool IsPermanentFailure()
        {
            // A returned message has no route and fails. A rejected message is attempted again. An exception
            // that wraps one of the two has decided what the broker's answer means, and is classified by itself.
            if (exception is PublishException publish)
            {
                return publish.IsReturn;
            }

            for (var current = exception; current is not null; current = current.InnerException)
            {
                switch (current)
                {
                    // One entry per broker node. A node that is only unreachable keeps the failure transient.
                    case AggregateException { InnerExceptions.Count: > 0 } nodes:
                        foreach (var node in nodes.InnerExceptions)
                        {
                            if (!node.IsPermanentFailure())
                            {
                                return false;
                            }
                        }

                        return true;

                    case AuthenticationFailureException
                    or AuthenticationException:
                        return true;

                    // The broker closed the channel or the connection and said why.
                    case OperationInterruptedException { ShutdownReason: { Initiator: ShutdownInitiator.Peer } reason }:
                        return reason.ReplyCode is not (Constants.ConnectionForced or Constants.InternalError);
                }
            }

            return exception
                is not (
                    IOException
                    or SocketException
                    or TimeoutException
                    or OperationCanceledException
                    or ObjectDisposedException
                    or ChannelClosedException
                    or OperationInterruptedException
                    or ConnectFailureException);
        }
    }
}
