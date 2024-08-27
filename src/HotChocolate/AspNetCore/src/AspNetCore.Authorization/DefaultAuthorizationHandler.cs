using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;
using IAuthorizationHandler = HotChocolate.Authorization.IAuthorizationHandler;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The default authorization implementation that uses Microsoft.AspNetCore.Authorization.
/// </summary>
internal sealed class DefaultAuthorizationHandler : IAuthorizationHandler
{
    private readonly IAuthorizationService _authSvc;
    private readonly IAuthorizationPolicyProvider _policyProvider;

    /// <summary>
    /// Initializes a new instance <see cref="DefaultAuthorizationHandler"/>.
    /// </summary>
    /// <param name="authorizationService">
    /// The authorization service.
    /// </param>
    /// <param name="authorizationPolicyProvider">
    /// The authorization policy provider.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="authorizationService"/> is <c>null</c>.
    /// <paramref name="authorizationPolicyProvider"/> is <c>null</c>.
    /// </exception>
    public DefaultAuthorizationHandler(
        IAuthorizationService authorizationService,
        IAuthorizationPolicyProvider authorizationPolicyProvider)
    {
        _authSvc = authorizationService ??
            throw new ArgumentNullException(nameof(authorizationService));
        _policyProvider = authorizationPolicyProvider ??
            throw new ArgumentNullException(nameof(authorizationPolicyProvider));
    }

    /// <summary>
    /// Authorize current directive using Microsoft.AspNetCore.Authorization.
    /// </summary>
    /// <param name="context">The current middleware context.</param>
    /// <param name="directive">The authorization directive.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// access the resolver data.
    /// </returns>
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken ct)
    {
        var userState = GetUserState(context.ContextData);
        var user = userState.User;
        bool authenticated;

        if (userState.IsAuthenticated.HasValue)
        {
            authenticated = userState.IsAuthenticated.Value;
        }
        else
        {
            // if the authenticated state is not yet set we will determine it and update the state.
            authenticated = user.Identities.Any(t => t.IsAuthenticated);
            userState = userState.SetIsAuthenticated(authenticated);
            SetUserState(context.ContextData, userState);
        }

        return await AuthorizeAsync(
                user,
                directive.Policy,
                directive.Roles,
                authenticated,
                context)
            .ConfigureAwait(false);
    }

    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken ct)
    {
        var userState = GetUserState(context.ContextData);
        var user = userState.User;
        bool authenticated;

        if (userState.IsAuthenticated.HasValue)
        {
            authenticated = userState.IsAuthenticated.Value;
        }
        else
        {
            // if the authenticated state is not yet set we will determine it and update the state.
            authenticated = user.Identities.Any(t => t.IsAuthenticated);
            userState = userState.SetIsAuthenticated(authenticated);
            SetUserState(context.ContextData, userState);
        }

        foreach (var directive in directives)
        {
            var result = await AuthorizeAsync(
                    user,
                    directive.Policy,
                    directive.Roles,
                    authenticated,
                    context)
                .ConfigureAwait(false);

            if (result is not AuthorizeResult.Allowed)
            {
                return result;
            }
        }

        return AuthorizeResult.Allowed;
    }

    private async ValueTask<AuthorizeResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string? policyName,
        IReadOnlyList<string>? roles,
        bool authenticated,
        object context)
    {
        var policyBuilder = new AuthorizationPolicyBuilder();

        if (!string.IsNullOrWhiteSpace(policyName))
        {
            var policy = await _policyProvider.GetPolicyAsync(policyName).ConfigureAwait(false);

            if (policy is not null)
            {
                policyBuilder = policyBuilder.Combine(policy);
            }
            else
            {
                return AuthorizeResult.PolicyNotFound;
            }
        }
        else
        {
            var defaultPolicy = await _policyProvider.GetDefaultPolicyAsync().ConfigureAwait(false);

            policyBuilder = policyBuilder.Combine(defaultPolicy);
        }

        if (roles is not null)
        {
            policyBuilder = policyBuilder.RequireRole(roles);
        }

        var finalPolicy = policyBuilder.Build();

        var result = await _authSvc.AuthorizeAsync(user, context, finalPolicy).ConfigureAwait(false);

        return result.Succeeded
            ? AuthorizeResult.Allowed
            : authenticated ? AuthorizeResult.NotAllowed : AuthorizeResult.NotAuthenticated;
    }

    private static UserState GetUserState(IDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(WellKnownContextData.UserState, out var value) &&
            value is UserState p)
        {
            return p;
        }

        throw new MissingStateException(
            "Authorization",
            WellKnownContextData.UserState,
            StateKind.Global);
    }

    private static void SetUserState(IDictionary<string, object?> contextData, UserState state)
        => contextData[WellKnownContextData.UserState] = state;

    private static bool FulfillsAnyRole(ClaimsPrincipal principal, IReadOnlyList<string> roles)
    {
        for (var i = 0; i < roles.Count; i++)
        {
            if (principal.IsInRole(roles[i]))
            {
                return true;
            }
        }

        return false;
    }
}
