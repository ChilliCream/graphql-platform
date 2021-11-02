using System;
using System.Threading;
using System.Threading.Tasks;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal class SeverMessageReceiver : IMessageReceiver
{
    public async ValueTask OnReceiveAsync(
        ISocketSession session,
        IMessage message,
        CancellationToken cancellationToken)
    {
        IMessageSender sender = session.Protocol.Sender;

        switch (message.Type)
        {
            case MessageTypes.Initialize:
                if (session.IsInitialized)
                {
                    throw new Exception("");
                }

                IMessage response = ConnectionAckMessage.Default;
                await sender.SendAsync(session, response, cancellationToken).ConfigureAwait(false);
                break;

            case MessageTypes.Pong:
                break;

            case MessageTypes.Subscribe:
                break;

            case MessageTypes.Complete:
                break;

            default:
                break;
        }
    }
}
