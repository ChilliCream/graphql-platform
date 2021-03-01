using System;
using StrawberryShake.Properties;

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

        /// <summary>
        /// Default unexpected server error
        /// <remarks>
        /// There was a unexpected error on the server
        /// </remarks>
        /// </summary>
        public static readonly ErrorOperationMessage UnexpectedServerError =
            new(Resources.ErrorOperationMessage_UnexpectedServerError);

        /// <summary>
        /// Default connection error
        /// <remarks>
        /// Connection initialization failed. Could not connect to server
        /// </remarks>
        /// </summary>
        public static readonly ErrorOperationMessage ConnectionInitializationError =
            new(Resources.ErrorOperationMessage_ConnectionInitializationError);

        /// <summary>
        /// Defaults response parsing error
        /// <remarks>
        /// Could not parse the response of the server
        /// </remarks>
        /// </summary>
        public static readonly ErrorOperationMessage ResponseParsingError =
            new(Resources.ErrorOperationMessage_ResponseParsingError);
    }
}
