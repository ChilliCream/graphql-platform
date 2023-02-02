namespace HotChocolate.Authorization;

/// <summary>
/// Defines when a policy shall be executed.
/// </summary>
public enum ApplyPolicy
{
    /// <summary>
    /// Before the resolver was executed.
    /// </summary>
    BeforeResolver = 0,

    /// <summary>
    /// After the resolver was executed.
    /// </summary>
    AfterResolver = 1,

    /// <summary>
    /// The policy is applied in the validation step before the execution.
    /// </summary>
    Validation = 2,
}
