namespace HotChocolate.Fusion.Authorization.InMemory;

/// <summary>
/// Configures the verdicts of the in-memory policies.
/// </summary>
public sealed class InMemoryPolicyBuilder
{
    private readonly Dictionary<string, Func<PolicyEvaluationContext, PolicyEvaluationEntry, PolicyVerdict>> _evaluators =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Allows every entry of the policy.
    /// </summary>
    /// <param name="policyName">
    /// The policy name.
    /// </param>
    /// <returns>
    /// The builder.
    /// </returns>
    public InMemoryPolicyBuilder Allow(string policyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        _evaluators[policyName] = static (_, _) => new PolicyVerdict(PolicyOutcome.Allowed, null, null);
        return this;
    }

    /// <summary>
    /// Denies every entry of the policy.
    /// </summary>
    /// <param name="policyName">
    /// The policy name.
    /// </param>
    /// <param name="reason">
    /// The reason that is recorded for auditing.
    /// </param>
    /// <returns>
    /// The builder.
    /// </returns>
    public InMemoryPolicyBuilder Deny(string policyName, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        _evaluators[policyName] = (_, _) => new PolicyVerdict(PolicyOutcome.Denied, reason, null);
        return this;
    }

    /// <summary>
    /// Registers the policy so that it resolves but leaves every entry unanswered.
    /// </summary>
    /// <param name="policyName">
    /// The policy name.
    /// </param>
    /// <returns>
    /// The builder.
    /// </returns>
    public InMemoryPolicyBuilder Unanswered(string policyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        _evaluators[policyName] = static (_, _) => new PolicyVerdict(PolicyOutcome.Unanswered, null, null);
        return this;
    }

    /// <summary>
    /// Allows each entry of the policy for which the delegate returns <c>true</c> and denies the others.
    /// </summary>
    /// <param name="policyName">
    /// The policy name.
    /// </param>
    /// <param name="evaluate">
    /// The delegate that decides an entry.
    /// </param>
    /// <returns>
    /// The builder.
    /// </returns>
    public InMemoryPolicyBuilder Evaluate(
        string policyName,
        Func<PolicyEvaluationContext, PolicyEvaluationEntry, bool> evaluate)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        ArgumentNullException.ThrowIfNull(evaluate);

        _evaluators[policyName] = (context, entry) => evaluate(context, entry)
            ? new PolicyVerdict(PolicyOutcome.Allowed, null, null)
            : new PolicyVerdict(PolicyOutcome.Denied, null, null);
        return this;
    }

    internal InMemoryPolicyProvider Build(InMemoryPolicyRecorder recorder)
        => new(new(_evaluators, StringComparer.Ordinal), recorder);
}
