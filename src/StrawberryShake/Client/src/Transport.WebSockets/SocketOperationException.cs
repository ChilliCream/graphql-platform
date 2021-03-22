using System;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Unexpected exception during a socket operation
    /// </summary>
    public class SocketOperationException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="SocketOperationException"/>
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public SocketOperationException(string message) : base(message)
        {
        }
    }
}
