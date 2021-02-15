namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="CancelledOperationMessage"/> is used to signal a socket operation that it
    /// is complete
    /// </summary>
    public class CancelledOperationMessage : OperationMessage
    {
        private CancelledOperationMessage()
            : base(OperationMessageType.Cancelled)
        {
        }

        public static readonly CancelledOperationMessage Default = new();
    }
}
