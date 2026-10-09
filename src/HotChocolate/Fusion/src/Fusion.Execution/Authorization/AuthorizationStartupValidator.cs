using System.Collections.Immutable;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Fails the startup of a gateway whose authorization requirements cannot be enforced.
/// </summary>
internal static class AuthorizationStartupValidator
{
    /// <summary>
    /// Validates the Schemes option, and for a schema that uses authorization also that a
    /// usable authentication scheme exists and that every policy name resolves.
    /// </summary>
    /// <param name="usage">
    /// The authorization requirements the schema uses.
    /// </param>
    /// <param name="options">
    /// The authorization options.
    /// </param>
    /// <param name="schemeResolver">
    /// The authentication schemes of the host.
    /// </param>
    /// <param name="policyResolver">
    /// The resolver that is asked for every policy name of the schema.
    /// </param>
    /// <param name="cancellationToken">
    /// The token that signals that the operation was aborted.
    /// </param>
    public static async ValueTask ValidateAsync(
        FusionAuthorizationUsage? usage,
        FusionAuthorizationOptions options,
        AuthenticationSchemeResolver schemeResolver,
        IPolicyResolver policyResolver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schemeResolver);
        ArgumentNullException.ThrowIfNull(policyResolver);

        if (options.DisableAuthorizationValidation)
        {
            return;
        }

        var registered = await ValidateConfigurationAsync(options, schemeResolver, cancellationToken)
            .ConfigureAwait(false);

        if (usage is null)
        {
            return;
        }

        if (usage.IsUsed)
        {
            ValidateUsage(options, schemeResolver, registered);
        }

        var policyNames = usage.PolicyNames;

        for (var i = 0; i < policyNames.Length; i++)
        {
            if (policyResolver.Resolve(policyNames[i], DirectiveNames.Policy.Name) is null)
            {
                throw ThrowHelper.PolicyNotResolved(DirectiveNames.Policy.Name, policyNames[i]);
            }
        }
    }

    private static async ValueTask<ImmutableArray<string>> ValidateConfigurationAsync(
        FusionAuthorizationOptions options,
        AuthenticationSchemeResolver schemeResolver,
        CancellationToken cancellationToken)
    {
        var registered = await schemeResolver.GetRegisteredAsync(cancellationToken).ConfigureAwait(false);

        if (options.Schemes is not { } listed)
        {
            return registered;
        }

        if (listed.IsEmpty)
        {
            throw ThrowHelper.EmptyAuthenticationSchemes();
        }

        if (!schemeResolver.HasLookup)
        {
            throw ThrowHelper.AuthenticationSchemesWithoutLookup();
        }

        for (var i = 0; i < listed.Length; i++)
        {
            if (!registered.Contains(listed[i]))
            {
                throw ThrowHelper.AuthenticationSchemeNotRegistered(listed[i]);
            }
        }

        return registered;
    }

    private static void ValidateUsage(
        FusionAuthorizationOptions options,
        AuthenticationSchemeResolver schemeResolver,
        ImmutableArray<string> registered)
    {
        if (!schemeResolver.HasLookup)
        {
            throw ThrowHelper.NoAuthenticationSchemeLookup();
        }

        if (options.Schemes is null && registered.IsEmpty)
        {
            throw ThrowHelper.NoAuthenticationSchemeRegistered();
        }
    }
}
