namespace ChilliCream.Nitro.Client;

/// <summary>
/// Represents the kind of a publish task update.
/// </summary>
public enum PublishUpdateKind
{
    Queued,
    Failed,
    Success,
    Ready,
    InProgress,
    WaitForApproval,
    Approved,
    Unknown
}
