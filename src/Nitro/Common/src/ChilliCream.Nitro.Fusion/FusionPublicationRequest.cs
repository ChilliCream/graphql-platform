namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Describes a Fusion configuration publication.
/// </summary>
public sealed record FusionPublicationRequest(
    FusionTarget Target,
    string Stage,
    string ConfigurationTag,
    IReadOnlyList<FusionSourceSchemaVersion> SourceSchemas,
    bool WaitForApproval,
    bool Force,
    TimeSpan OperationTimeout,
    TimeSpan ApprovalTimeout);
