namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class KeepConnectionAliveMessage
        : OperationMessage
    {
        public KeepConnectionAliveMessage()
            : base(MessageTypes.Connection.KeepAlive)
        {
        }

        public KeepConnectionAliveMessage Default { get; } =
            new KeepConnectionAliveMessage();
    }
}
