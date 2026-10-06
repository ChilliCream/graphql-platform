using HotChocolate.Types;

namespace HotChocolate.Fusion.Authorization.InMemory;

/// <summary>
/// Provides the configured in-memory policies for the <c>@policy</c> directive. Policy names
/// that were not configured return <c>null</c>.
/// </summary>
public sealed class InMemoryPolicyProvider : IPolicyProvider
{
    private readonly Dictionary<string, Func<PolicyEvaluationContext, PolicyEvaluationEntry, PolicyVerdict>> _evaluators;
    private readonly InMemoryPolicyRecorder _recorder;

    internal InMemoryPolicyProvider(
        Dictionary<string, Func<PolicyEvaluationContext, PolicyEvaluationEntry, PolicyVerdict>> evaluators,
        InMemoryPolicyRecorder recorder)
    {
        _evaluators = evaluators;
        _recorder = recorder;
    }

    /// <inheritdoc />
    public IPolicy? GetPolicy(string policyName, string directiveName)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        ArgumentNullException.ThrowIfNull(directiveName);

        if (directiveName is not DirectiveNames.Policy.Name)
        {
            return null;
        }

        return _evaluators.TryGetValue(policyName, out var evaluate)
            ? new InMemoryPolicy(policyName, evaluate, _recorder)
            : null;
    }
}
