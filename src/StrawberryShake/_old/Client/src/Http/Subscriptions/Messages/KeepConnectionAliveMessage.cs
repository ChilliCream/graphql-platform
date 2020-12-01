using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class KeepConnectionAliveMessage
        : OperationMessage
    {
        private KeepConnectionAliveMessage()
            : base(MessageTypes.Connection.KeepAlive)
        {
        }

        public static KeepConnectionAliveMessage Default { get; } =
            new KeepConnectionAliveMessage();
    }
}
