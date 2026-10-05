namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The decision a policy reached for a single <see cref="PolicyEvaluationEntry"/>.
/// </summary>
public enum PolicyOutcome
{
    /// <summary>
    /// The policy did not decide the entry. The entry is denied for the client.
    /// </summary>
    Unanswered = 0,

    /// <summary>
    /// The policy allowed the entry.
    /// </summary>
    Allowed = 1,

    /// <summary>
    /// The policy denied the entry.
    /// </summary>
    Denied = 2
}
