namespace HotChocolate.Fusion.Authorization.InMemory;

/// <summary>
/// An entry that an in-memory policy was asked about.
/// </summary>
/// <param name="PolicyName">
/// The name of the policy.
/// </param>
/// <param name="Entry">
/// The entry that was evaluated.
/// </param>
public readonly record struct InMemoryPolicyRecord(
    string PolicyName,
    PolicyEvaluationEntry Entry);
