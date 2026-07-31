using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents a Fusion deployment to Nitro. A deployment publishes to one Nitro stage and can be
/// restricted to a single Aspire environment.
/// </summary>
public sealed class FusionDeploymentResource(
    string name,
    NitroPublishTargetResource nitro)
    : Resource(name)
{
    internal NitroPublishTargetResource Nitro { get; } = nitro;

    internal string? EnvironmentName { get; set; }

    internal string? StageName { get; set; }

    internal ParameterResource? StageParameter { get; set; }

    internal string? CompositionEnvironmentName { get; set; }

    internal string? ConfigurationTag { get; set; }

    internal ParameterResource? ConfigurationTagParameter { get; set; }

    internal bool WaitForApproval { get; set; }

    internal bool Force { get; set; }

    internal TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(15);

    internal TimeSpan ApprovalTimeout { get; set; } = TimeSpan.FromHours(2);
}
