namespace Mocha;

/// <summary>
/// Thrown when a transport could not hand a message to its broker because the broker was unreachable or
/// did not answer in time. The broker may or may not have received the message.
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
