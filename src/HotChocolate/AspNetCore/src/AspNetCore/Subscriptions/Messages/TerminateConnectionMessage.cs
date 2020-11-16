namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class TerminateConnectionMessage
        : OperationMessage
    {
        public TerminateConnectionMessage()
            : base(MessageTypes.Connection.Terminate)
        {
        }

        public static TerminateConnectionMessage Default { get; } =
            new TerminateConnectionMessage();
    }
}
