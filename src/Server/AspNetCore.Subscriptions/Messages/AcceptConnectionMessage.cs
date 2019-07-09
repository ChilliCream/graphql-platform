namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class AcceptConnectionMessage
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
