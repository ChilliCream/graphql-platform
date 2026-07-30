namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// The base exception for failures reported by the Fusion deployment workflow.
/// </summary>
public class FusionDeploymentException : Exception
{
    public FusionDeploymentException(string message)
        : base(message)
    {
    }

    public FusionDeploymentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
