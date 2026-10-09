using System.Collections.Immutable;
using HotChocolate.Fusion.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace HotChocolate.Fusion.AspNetCore;

internal sealed class AspNetCoreAuthenticationSchemeLookup(IAuthenticationSchemeProvider? schemeProvider)
    : IAuthenticationSchemeLookup
{
    private const string JwtBearerHandlerTypeName = "Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler";
    private const string NegotiateHandlerTypeName = "Microsoft.AspNetCore.Authentication.Negotiate.NegotiateHandler";
    private const string BearerChallenge = "Bearer";
    private const string NegotiateChallenge = "Negotiate";

    public async ValueTask<ImmutableArray<string>> GetSchemeNamesAsync(CancellationToken cancellationToken)
    {
        if (schemeProvider is null)
        {
            return [];
        }

        var schemes = await schemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

        return [.. schemes.Select(scheme => scheme.Name)];
    }

    public async ValueTask<string?> GetChallengeAsync(string schemeName, CancellationToken cancellationToken)
    {
        if (schemeProvider is null)
        {
            return null;
        }

        var scheme = await schemeProvider.GetSchemeAsync(schemeName).ConfigureAwait(false);

        return scheme?.HandlerType.FullName switch
        {
            JwtBearerHandlerTypeName => BearerChallenge,
            NegotiateHandlerTypeName => NegotiateChallenge,
            _ => null
        };
    }
}
