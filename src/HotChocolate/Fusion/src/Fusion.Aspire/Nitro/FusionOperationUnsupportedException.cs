namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The Nitro server does not know an operation that the Fusion deployment workflow sends.
/// </summary>
internal sealed class FusionOperationUnsupportedException : FusionDeploymentException
{
    public FusionOperationUnsupportedException(string message)
        : base(message)
    {
    }
}
