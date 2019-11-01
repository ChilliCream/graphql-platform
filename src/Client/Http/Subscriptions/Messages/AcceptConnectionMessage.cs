namespace StrawberryShake.Http.Subscriptions
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
