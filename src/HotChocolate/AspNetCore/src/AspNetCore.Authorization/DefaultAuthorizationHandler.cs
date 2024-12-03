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
    private readonly IAuthorizationPolicyProvider _authorizationPolicyProvider;
    private readonly AuthorizationPolicyCache _authorizationPolicyCache;
    private readonly bool _canCachePolicies;

    /// <summary>
    /// Initializes a new instance <see cref="DefaultAuthorizationHandler"/>.
    /// </summary>
    /// <param name="authorizationService">
    /// The authorization service.
    /// </param>
    /// <param name="authorizationPolicyProvider">
    /// The authorization policy provider.
    /// </param>
    /// <param name="authorizationPolicyCache">
    /// The authorization policy cache.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="authorizationService"/> is <c>null</c>.
    /// <paramref name="authorizationPolicyCache"/> is <c>null</c>.
    /// </exception>
    public DefaultAuthorizationHandler(
        IAuthorizationService authorizationService,
        IAuthorizationPolicyProvider authorizationPolicyProvider,
        AuthorizationPolicyCache authorizationPolicyCache)
    {
        _authSvc = authorizationService ??
            throw new ArgumentNullException(nameof(authorizationService));
        _authorizationPolicyProvider = authorizationPolicyProvider ??
            throw new ArgumentNullException(nameof(authorizationPolicyProvider));
        _authorizationPolicyCache = authorizationPolicyCache ??
            throw new ArgumentNullException(nameof(authorizationPolicyCache));

        _canCachePolicies = _authorizationPolicyProvider.AllowsCachingPolicies;
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
                directive,
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
                    directive,
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
        AuthorizeDirective directive,
        bool authenticated,
        object context)
    {
        try
        {
            AuthorizationPolicy? authorizationPolicy = null;

            if (_canCachePolicies)
            {
                authorizationPolicy = _authorizationPolicyCache.LookupPolicy(directive);
            }

            if (authorizationPolicy is null)
            {
                authorizationPolicy = await BuildAuthorizationPolicy(directive.Policy, directive.Roles);

                if (_canCachePolicies)
                {
                    _authorizationPolicyCache.CachePolicy(directive, authorizationPolicy);
                }
            }

            var result = await _authSvc.AuthorizeAsync(user, context, authorizationPolicy).ConfigureAwait(false);

            return result.Succeeded
                ? AuthorizeResult.Allowed
                : authenticated ? AuthorizeResult.NotAllowed : AuthorizeResult.NotAuthenticated;
        }
        catch (MissingAuthorizationPolicyException)
        {
            return AuthorizeResult.PolicyNotFound;
        }
    }

    private async Task<AuthorizationPolicy> BuildAuthorizationPolicy(
        string? policyName,
        IReadOnlyList<string>? roles)
    {
        var policyBuilder = new AuthorizationPolicyBuilder();

        if (!string.IsNullOrWhiteSpace(policyName))
        {
            var policy = await _authorizationPolicyProvider.GetPolicyAsync(policyName).ConfigureAwait(false);

            if (policy is not null)
            {
                policyBuilder = policyBuilder.Combine(policy);
            }
            else
            {
                throw new MissingAuthorizationPolicyException(policyName);
            }
        }
        else
        {
            var defaultPolicy = await _authorizationPolicyProvider.GetDefaultPolicyAsync().ConfigureAwait(false);

            policyBuilder = policyBuilder.Combine(defaultPolicy);
        }

        if (roles is not null)
        {
            policyBuilder = policyBuilder.RequireRole(roles);
        }

        return policyBuilder.Build();
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
}
