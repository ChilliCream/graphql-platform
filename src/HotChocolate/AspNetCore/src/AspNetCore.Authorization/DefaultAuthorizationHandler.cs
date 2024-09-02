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
    private readonly AuthorizationPolicyCache _policyCache;

    /// <summary>
    /// Initializes a new instance <see cref="DefaultAuthorizationHandler"/>.
    /// </summary>
    /// <param name="authorizationService">
    /// The authorization service.
    /// </param>
    /// <param name="policyCache">
    /// The authorization policy cache.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="authorizationService"/> is <c>null</c>.
    /// <paramref name="policyCache"/> is <c>null</c>.
    /// </exception>
    public DefaultAuthorizationHandler(
        IAuthorizationService authorizationService,
        AuthorizationPolicyCache policyCache)
    {
        _authSvc = authorizationService ??
            throw new ArgumentNullException(nameof(authorizationService));
        _policyCache = policyCache ??
            throw new ArgumentNullException(nameof(policyCache));
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
            var combinedPolicy = await _policyCache.GetOrCreatePolicyAsync(directive);

            var result = await _authSvc.AuthorizeAsync(user, context, combinedPolicy).ConfigureAwait(false);

            return result.Succeeded
                ? AuthorizeResult.Allowed
                : authenticated ? AuthorizeResult.NotAllowed : AuthorizeResult.NotAuthenticated;
        }
        catch (MissingAuthorizationPolicyException)
        {
            return AuthorizeResult.PolicyNotFound;
        }
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
