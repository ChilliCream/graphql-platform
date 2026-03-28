namespace ChilliCream.Nitro.Client.Exceptions;

/// <summary>
/// Represents an error returned by or encountered while communicating with Nitro Cloud APIs.
/// </summary>
public class NitroClientException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NitroClientException"/>.
    /// </summary>
    public NitroClientException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NitroClientException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NitroClientException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NitroClientException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The root cause exception.</param>
    public NitroClientException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
