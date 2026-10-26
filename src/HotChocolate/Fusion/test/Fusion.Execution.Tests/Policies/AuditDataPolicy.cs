using System.Collections.Immutable;

namespace HotChocolate.Fusion.Policies;

internal sealed class AuditDataPolicy : IPolicy, IPolicyProvider
{
    public IPolicy? GetPolicy(string policyName, string directiveName)
        => policyName == "audited" ? this : null;

    public ValueTask EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        foreach (ref readonly var entry in context.Entries)
        {
            context.Deny(
                in entry,
                "not the owner",
                ImmutableDictionary<string, string>.Empty.Add("rule", "owner"));
        }

        return ValueTask.CompletedTask;
    }
}
