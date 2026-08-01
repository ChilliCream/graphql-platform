using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents a Nitro stage that a distributed application publishes its Fusion configuration to.
/// Every invocation publishes to exactly one declared stage of a Nitro api.
/// </summary>
public sealed class FusionStageResource(
    string name,
    string stageName,
    NitroPublishTargetResource nitro)
    : Resource(name)
{
    internal NitroPublishTargetResource Nitro { get; } = nitro;

    /// <summary>
    /// The name of the Nitro stage. It is also the value that selects this stage for an invocation.
    /// </summary>
    internal string StageName { get; } = stageName;

    internal string? CompositionEnvironmentName { get; set; }

    internal bool WaitForApproval { get; set; }

    internal bool Force { get; set; }

    internal static TimeSpan OperationTimeout { get; } = TimeSpan.FromMinutes(15);

    internal static TimeSpan ApprovalTimeout { get; } = TimeSpan.FromHours(2);
}
