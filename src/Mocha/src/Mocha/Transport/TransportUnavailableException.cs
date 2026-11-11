namespace Mocha;

/// <summary>
/// Thrown when a transport could not hand a message to its broker in time: the broker was unreachable, did
/// not answer, or did not accept the message. The broker may or may not have received the message, unless the
/// message of the exception says that it was not sent.
/// </summary>
public sealed class TransportUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TransportUnavailableException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The failure of the last attempt, or <c>null</c> when there was none.</param>
    public TransportUnavailableException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
