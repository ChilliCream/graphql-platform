using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Authorization;

internal sealed class UnresolvedPolicy(string directiveName, string? policyName) : IPolicy
{
    public ValueTask EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
        => throw ThrowHelper.PolicyNotResolved(directiveName, policyName);
}
