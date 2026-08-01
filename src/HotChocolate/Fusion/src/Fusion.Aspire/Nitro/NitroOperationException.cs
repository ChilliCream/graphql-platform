namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Nitro answered an operation with GraphQL errors.
/// </summary>
internal sealed class NitroOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NitroOperationException"/>.
    /// </summary>
    /// <param name="message">
    /// The message that describes the errors that Nitro reported.
    /// </param>
    /// <param name="errorCode">
    /// The code that Nitro reported for the first error, or <c>null</c> when the error carries
    /// no code.
    /// </param>
    public NitroOperationException(string message, string? errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the code that Nitro reported for the first error, or <c>null</c> when the error
    /// carries no code.
    /// </summary>
    public string? ErrorCode { get; }
}
