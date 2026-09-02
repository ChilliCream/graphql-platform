using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Represents a stage of a Nitro API.
/// </summary>
public sealed class NitroStageResource(
    string name,
    string stageName,
    NitroApiResource api)
    : Resource(name)
{
    internal NitroApiResource Api { get; } = api;

    /// <summary>
    /// Gets the name of the Nitro stage.
    /// </summary>
    public string StageName { get; } = stageName;

    internal bool WaitForApproval { get; set; }

    internal bool Force { get; set; }

    internal static TimeSpan OperationTimeout { get; } = TimeSpan.FromMinutes(15);

    internal static TimeSpan ApprovalTimeout { get; } = TimeSpan.FromHours(2);
}
