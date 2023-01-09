using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using IAuthorizationHandler = HotChocolate.Authorization.IAuthorizationHandler;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The default authorization implementation that uses Microsoft.AspNetCore.Authorization.
/// </summary>
internal sealed class DefaultAuthorizationHandler : IAuthorizationHandler
{
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
        if (!TryGetAuthenticatedPrincipal(context, out var principal))
        {
            return AuthorizeResult.NotAuthenticated;
        }

        if (IsInAnyRole(principal, directive.Roles))
        {
            if (NeedsPolicyValidation(directive))
            {
                return await AuthorizeWithPolicyAsync(
                        context.Services,
                        directive,
                        principal,
                        context)
                    .ConfigureAwait(false);
            }

            return AuthorizeResult.Allowed;
        }

        return AuthorizeResult.NotAllowed;
    }

    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedPrincipal(context, out var principal))
        {
            return AuthorizeResult.NotAuthenticated;
        }

        foreach (var directive in directives)
        {
            if (IsInAnyRole(principal, directive.Roles))
            {
                if (NeedsPolicyValidation(directive))
                {
                    var result = await AuthorizeWithPolicyAsync(
                            context.Services,
                            directive,
                            principal,
                            directive)
                        .ConfigureAwait(false);

                    if (result is not AuthorizeResult.Allowed)
                    {
                        return result;
                    }
                }
            }
        }

        return AuthorizeResult.Allowed;
    }


#if NETSTANDARD2_0
    private static bool TryGetAuthenticatedPrincipal(
        IMiddlewareContext context,
        out ClaimsPrincipal? principal)
#else
    private static bool TryGetAuthenticatedPrincipal(
        IMiddlewareContext context,
        [NotNullWhen(true)] out ClaimsPrincipal? principal)
#endif
    {
        if (context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var o) &&
            o is ClaimsPrincipal p &&
            p.Identities.Any(t => t.IsAuthenticated))
        {
            principal = p;
            return true;
        }

        principal = null;
        return false;
    }

#if NETSTANDARD2_0
    private static bool TryGetAuthenticatedPrincipal(
        AuthorizationContext context,
        out ClaimsPrincipal? principal)
#else
    private static bool TryGetAuthenticatedPrincipal(
        AuthorizationContext context,
        [NotNullWhen(true)] out ClaimsPrincipal? principal)
#endif
    {
        if (context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var o) &&
            o is ClaimsPrincipal p &&
            p.Identities.Any(t => t.IsAuthenticated))
        {
            principal = p;
            return true;
        }

        principal = null;
        return false;
    }

    private static bool IsInAnyRole(
        IPrincipal principal,
        IReadOnlyList<string>? roles)
    {
        if (roles == null || roles.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < roles.Count; i++)
        {
            if (principal.IsInRole(roles[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NeedsPolicyValidation(AuthorizeDirective directive)
        => directive.Roles == null ||
            directive.Roles.Count == 0 ||
            !string.IsNullOrEmpty(directive.Policy);

    private static async Task<AuthorizeResult> AuthorizeWithPolicyAsync(
        IServiceProvider services,
        AuthorizeDirective directive,
        ClaimsPrincipal principal,
        object resource)
    {
        var authorizeService = services.GetService<IAuthorizationService>();
        var policyProvider = services.GetService<IAuthorizationPolicyProvider>();

        if (authorizeService == null || policyProvider == null)
        {
            // authorization service is not configured so the user is
            // authorized with the previous checks.
            return string.IsNullOrWhiteSpace(directive.Policy)
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed;
        }

        AuthorizationPolicy? policy = null;

        if ((directive.Roles is null || directive.Roles.Count == 0) &&
            string.IsNullOrWhiteSpace(directive.Policy))
        {
            policy = await policyProvider.GetDefaultPolicyAsync()
                .ConfigureAwait(false);

            if (policy == null)
            {
                return AuthorizeResult.NoDefaultPolicy;
            }
        }
        else if (!string.IsNullOrWhiteSpace(directive.Policy))
        {
            policy = await policyProvider.GetPolicyAsync(directive.Policy)
                .ConfigureAwait(false);

            if (policy == null)
            {
                return AuthorizeResult.PolicyNotFound;
            }
        }

        if (policy is not null)
        {
            var result =
                await authorizeService
                    .AuthorizeAsync(principal, resource, policy)
                    .ConfigureAwait(false);

            return result.Succeeded
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed;
        }

        return AuthorizeResult.NotAllowed;
    }
}
