namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The built-in <c>fusion.authenticated</c> policy: allows the request when
/// <see cref="IPolicyContext.User"/> carries an authenticated identity.
/// </summary>
internal sealed class AuthenticatedPolicy : IPolicy
{
    public string Name => BuiltInPolicyNames.Authenticated;

    public PolicyRequirements Requirements => PolicyRequirements.Empty;

    public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Deny(0);
        }

        return ValueTask.CompletedTask;
    }
}
