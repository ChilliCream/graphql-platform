namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="CompleteOperationMessage "/> is used to signal a socket operation that it
    /// is complete
    /// </summary>
    public class CompleteOperationMessage : OperationMessage
    {
        private CompleteOperationMessage() : base(OperationMessageType.Complete)
        {
        }

        public static readonly CompleteOperationMessage Default = new();
    }
}
