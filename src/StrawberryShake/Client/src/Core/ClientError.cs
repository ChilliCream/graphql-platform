namespace StrawberryShake;

/// <summary>
/// Represents a query error.
/// </summary>
public class ClientError : IClientError
{
    /// <summary>
    /// Initializes a new instance of <see cref="ClientError"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    /// <param name="path">The field path where the error happened.</param>
    /// <param name="locations">A location reference to the query document.</param>
    /// <param name="exception">An associated exception.</param>
    /// <param name="extensions">Additional error data.</param>
    public ClientError(
        string message,
        string? code = null,
        IReadOnlyList<object>? path = null,
        IReadOnlyList<Location>? locations = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Code = code;
        Path = path;
        Locations = locations;
        Exception = exception;
        Extensions = extensions;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <inheritdoc />
    public string? Code { get; }

    /// <inheritdoc />
    public IReadOnlyList<object>? Path { get; }

    /// <inheritdoc />
    public IReadOnlyList<Location>? Locations { get; }

    /// <inheritdoc />
    public Exception? Exception { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Extensions { get; }
}
