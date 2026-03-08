namespace Mocha.Events;

/// <summary>
/// Represents an error that occurred on the remote handler side during request-reply processing.
/// </summary>
public sealed class RemoteErrorException(
    string errorCode,
    string? errorMessage,
    string? messageId,
    string? correlationId) : Exception($"Remote error occurred: {errorCode} - {errorMessage}")
{
    /// <summary>
    /// Gets the error code returned by the remote handler.
    /// </summary>
    public string ErrorCode => errorCode;

    /// <summary>
    /// Gets the error message returned by the remote handler, or <c>null</c>.
    /// </summary>
    public string? ErrorMessage => errorMessage;

    /// <summary>
    /// Gets the message identifier of the failed request, or <c>null</c>.
    /// </summary>
    public string? MessageId => messageId;

    /// <summary>
    /// Gets the correlation identifier of the failed request, or <c>null</c>.
    /// </summary>
    public string? CorrelationId => correlationId;
}
