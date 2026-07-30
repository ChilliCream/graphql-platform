namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// A source schema name and version already exist with different content.
/// </summary>
public sealed class FusionIdentityCollisionException : FusionDeploymentException
{
    public FusionIdentityCollisionException(string message)
        : base(message)
    {
    }
}
