namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The built-in <c>fusion.deny</c> policy: always denies. Backs the translation of an Apollo
/// <c>@requiresScopes(scopes: [])</c> application, an OR over zero scope alternatives and
/// therefore never satisfied, into an explicit deny instead of an empty, undefined policy-name
/// list.
/// </summary>
internal sealed class DenyPolicy : IPolicy
{
    public string Name => BuiltInPolicyNames.Deny;

    public PolicyRequirements Requirements => PolicyRequirements.Empty;

    public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
    {
        context.Deny(0);

        return ValueTask.CompletedTask;
    }
}
