namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A remote operation may have changed state, but its result could not be verified.
/// </summary>
internal sealed class FusionIndeterminateStateException : FusionDeploymentException
{
    public FusionIndeterminateStateException(string message, string? requestId = null)
        : base(message)
    {
        RequestId = requestId;
    }

    public FusionIndeterminateStateException(
        string message,
        Exception innerException,
        string? requestId = null)
        : base(message, innerException)
    {
        RequestId = requestId;
    }

    /// <summary>
    /// Gets the remote publication request identifier when one is known.
    /// </summary>
    public string? RequestId { get; }
}
