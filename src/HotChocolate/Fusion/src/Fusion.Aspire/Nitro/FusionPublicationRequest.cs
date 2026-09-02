namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes a Fusion configuration publication.
/// </summary>
internal sealed record FusionPublicationRequest(
    FusionTarget Target,
    string Stage,
    string ConfigurationTag,
    IReadOnlyList<FusionSourceSchemaVersion> SourceSchemas,
    bool WaitForApproval,
    bool Force,
    TimeSpan OperationTimeout,
    TimeSpan ApprovalTimeout);
