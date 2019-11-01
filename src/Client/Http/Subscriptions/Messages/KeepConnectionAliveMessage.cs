namespace StrawberryShake.Http.Subscriptions
{
    public sealed class KeepConnectionAliveMessage
        : OperationMessage
    {
        public KeepConnectionAliveMessage()
            : base(MessageTypes.Connection.KeepAlive)
        {
        }

        public static KeepConnectionAliveMessage Default { get; } =
            new KeepConnectionAliveMessage();
    }
}
