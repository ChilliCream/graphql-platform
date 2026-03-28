namespace ChilliCream.Nitro.Client;

/// <summary>
/// Represents a publish task update.
/// </summary>
public sealed record PublishUpdate(
    PublishUpdateKind Kind,
    int? QueuePosition = null,
    IReadOnlyList<MutationError>? Errors = null,
    IReadOnlyList<MutationError>? DeploymentErrors = null);
