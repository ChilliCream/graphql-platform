namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A source schema name and version already exist with different content.
/// </summary>
internal sealed class FusionIdentityCollisionException : FusionDeploymentException
{
    public FusionIdentityCollisionException(string message)
        : base(message)
    {
    }
}
