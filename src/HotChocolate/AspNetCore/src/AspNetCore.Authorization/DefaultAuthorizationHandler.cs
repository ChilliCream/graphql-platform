using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
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
        var checkRoles = roles is { Count: > 0, };
        var checkPolicy = !string.IsNullOrWhiteSpace(policyName);

        // if the current directive has neither roles nor policies specified we will check if there
        // is a default policy specified.
        if (!checkRoles && !checkPolicy)
        {
            var policy = await _policyProvider.GetDefaultPolicyAsync().ConfigureAwait(false);

            // if there is no default policy specified we will check if at least one of the
            // identities are authenticated to authorize the user.
            if (policy is null)
            {
                return authenticated
                    ? AuthorizeResult.Allowed
                    : AuthorizeResult.NoDefaultPolicy;
            }

            // if we find a default policy we will use this to authorize the access to a resource.
            var result = await _authSvc.AuthorizeAsync(user, context, policy).ConfigureAwait(false);
            return result.Succeeded
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed;
        }

        // We first check if the user fulfills any of the specified roles.
        // If no role was specified the user fulfills them.
        if (!checkRoles || FulfillsAnyRole(user, roles!))
        {
            if (!checkPolicy)
            {
                // The user fulfills one or all of the roles and no policy check was required.
                return AuthorizeResult.Allowed;
            }

            // If a policy name was supplied we will try to resolve the policy
            // and authorize with it.
            var policy = await _policyProvider.GetPolicyAsync(policyName!).ConfigureAwait(false);

            if (policy is null)
            {
                return AuthorizeResult.PolicyNotFound;
            }

            var result = await _authSvc.AuthorizeAsync(user, context, policy).ConfigureAwait(false);
            return result.Succeeded
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed;
        }

        return AuthorizeResult.NotAllowed;
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
