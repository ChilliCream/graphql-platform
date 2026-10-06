namespace HotChocolate.Fusion.Authorization.InMemory;

internal sealed class InMemoryPolicy : IPolicy
{
    private readonly string _policyName;
    private readonly Func<PolicyEvaluationContext, PolicyEvaluationEntry, PolicyVerdict> _evaluate;
    private readonly InMemoryPolicyRecorder _recorder;

    public InMemoryPolicy(
        string policyName,
        Func<PolicyEvaluationContext, PolicyEvaluationEntry, PolicyVerdict> evaluate,
        InMemoryPolicyRecorder recorder)
    {
        _policyName = policyName;
        _evaluate = evaluate;
        _recorder = recorder;
    }

    public ValueTask EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        foreach (ref readonly var entry in context.Entries)
        {
            _recorder.Record(_policyName, in entry);

            var verdict = _evaluate(context, entry);

            switch (verdict.Outcome)
            {
                case PolicyOutcome.Allowed:
                    context.Allow(in entry);
                    break;

                case PolicyOutcome.Denied:
                    context.Deny(in entry, verdict.Reason);
                    break;
            }
        }

        return ValueTask.CompletedTask;
    }
}
