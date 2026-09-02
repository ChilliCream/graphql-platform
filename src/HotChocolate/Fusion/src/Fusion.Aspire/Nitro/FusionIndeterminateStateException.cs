namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A remote operation may have changed state, but its result could not be verified.
/// </summary>
internal sealed class FusionIndeterminateStateException : FusionDeploymentException
{
    public FusionIndeterminateStateException(string message, string? requestId = null)
        : base(IncludeRequestId(message, requestId))
    {
        RequestId = requestId;
    }

    public FusionIndeterminateStateException(
        string message,
        Exception innerException,
        string? requestId = null)
        : base(IncludeRequestId(message, requestId), innerException)
    {
        RequestId = requestId;
    }

    /// <summary>
    /// Gets the remote publication request identifier when one is known.
    /// </summary>
    public string? RequestId { get; }

    private static string IncludeRequestId(string message, string? requestId)
        => string.IsNullOrWhiteSpace(requestId)
            ? message
            : $"{message} Nitro request ID: '{requestId}'.";
}
