using Aspire.Hosting.ApplicationModel;

namespace ChilliCream.Nitro.Aspire;

/// <summary>
/// Represents an environment-specific Fusion deployment to Nitro.
/// </summary>
public sealed class FusionDeploymentResource(
    string name,
    NitroResource nitro)
    : Resource(name)
{
    internal NitroResource Nitro { get; } = nitro;

    internal string? EnvironmentName { get; set; }

    internal string? StageName { get; set; }

    internal string? ConfigurationTag { get; set; }

    internal ParameterResource? ConfigurationTagParameter { get; set; }

    internal bool UseGitCommitAsSourceVersion { get; set; }

    internal bool WaitForApproval { get; set; }

    internal bool Force { get; set; }

    internal TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(15);

    internal TimeSpan ApprovalTimeout { get; set; } = TimeSpan.FromHours(2);
}
