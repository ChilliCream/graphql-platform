using System;

namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="ErrorOperationMessage"/> is used to transport any connection error to the
    /// socket operation
    /// </summary>
    public class ErrorOperationMessage : OperationMessage
    {
        public ErrorOperationMessage(string message)
            : base(OperationMessageType.Error)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; }

        public static readonly ErrorOperationMessage UnexpectedServerError =
            new("Unexpected Server Error");

        public static readonly ErrorOperationMessage ConnectionError =
            new("Connection initialization failed. Could not connect to server");
    }
}
