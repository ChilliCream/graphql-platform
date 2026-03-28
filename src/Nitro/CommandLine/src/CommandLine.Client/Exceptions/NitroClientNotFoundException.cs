namespace ChilliCream.Nitro.Client.Exceptions;

/// <summary>
/// Represents a resource-not-found failure while communicating with Nitro Cloud APIs.
/// </summary>
public sealed class NitroClientNotFoundException : NitroClientException
{
    /// <summary>
    /// Initializes a new instance of <see cref="NitroClientNotFoundException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NitroClientNotFoundException(string message) : base(message)
    {
    }
}
