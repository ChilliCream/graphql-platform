namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The built-in policy of the <c>@authenticated</c> directive. It allows every entry when the
/// principal is authenticated and denies every entry otherwise.
/// </summary>
public sealed class AuthenticatedPolicy : IPolicy
{
    private AuthenticatedPolicy()
    {
    }

    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static AuthenticatedPolicy Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var isAuthenticated = context.User.Identity?.IsAuthenticated is true;

        foreach (ref readonly var entry in context.Entries)
        {
            if (isAuthenticated)
            {
                context.Allow(in entry);
            }
            else
            {
                context.Deny(in entry);
            }
        }

        return ValueTask.CompletedTask;
    }
}
