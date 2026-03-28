namespace ChilliCream.Nitro.Client.Exceptions;

/// <summary>
/// Represents an authorization failure while communicating with Nitro Cloud APIs.
/// </summary>
public sealed class NitroClientAuthorizationException : NitroClientException
{
    /// <summary>
    /// Initializes a new instance of <see cref="NitroClientAuthorizationException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NitroClientAuthorizationException(string message) : base(message)
    {
    }

    public NitroClientAuthorizationException()
    {
    }
}
