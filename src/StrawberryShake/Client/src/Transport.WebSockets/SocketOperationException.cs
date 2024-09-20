namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Unexpected exception during a socket operation
/// </summary>
public sealed class SocketOperationException : Exception
{
    /// <summary>
    /// Creates a new <see cref="SocketOperationException"/>
    /// </summary>
    public SocketOperationException()
    {
    }

    /// <summary>
    /// Creates a new <see cref="SocketOperationException"/>
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SocketOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="SocketOperationException"/>
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">THe inner exception.</param>
    public SocketOperationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
