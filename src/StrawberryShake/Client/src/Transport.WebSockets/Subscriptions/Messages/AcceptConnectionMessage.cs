using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class AcceptConnectionMessage
        : OperationMessage
    {
        public AcceptConnectionMessage()
            : base(MessageTypes.Connection.Accept)
        {
        }

        public static AcceptConnectionMessage Default { get; } =
            new AcceptConnectionMessage();
    }
}
