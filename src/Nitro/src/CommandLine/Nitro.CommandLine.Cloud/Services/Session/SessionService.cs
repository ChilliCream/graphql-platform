using System.IdentityModel.Tokens.Jwt;
using ChilliCream.Nitro.CLI.Auth;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Helpers;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Duende.IdentityModel.OidcClient.Results;
using static ChilliCream.Nitro.CLI.WellKnownClaims;

namespace ChilliCream.Nitro.CLI;

internal class SessionService : ISessionService
{
    private readonly IConfigurationService _configurationService;

    public SessionService(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public Session? Session { get; private set; }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Session is { Tokens.IdToken : { } idToken, IdentityServer: { } authority })
            {
                await CreateClient(o =>
                    {
                        o.Browser = new CliBrowser();
                        o.Authority = authority;
                    })
                    .LogoutAsync(
                        new LogoutRequest
                        {
                            BrowserDisplayMode = DisplayMode.Hidden, IdTokenHint = idToken
                        },
                        cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // even if the logout fails, we need to clear the session
        }

        await _configurationService.ResetAsync<Session>(cancellationToken);
    }

    public async Task<Session> SelectWorkspaceAsync(
        Workspace workspace,
        CancellationToken cancellationToken)
    {
        await EnsureSessionAsync(cancellationToken);

        Session!.Workspace = workspace;

        await _configurationService.SaveAsync(Session, cancellationToken);

        return Session;
    }

    public async Task<Session?> LoadSessionAsync(CancellationToken cancellationToken)
    {
        Session = await _configurationService.GetAsync<Session>(cancellationToken);

        if (Session?.Tokens is { RefreshToken: { } refreshToken, ExpiresAt: var expiresAt } &&
            expiresAt < DateTimeOffset.Now.AddMinutes(-1))
        {
            await RefreshTokenAsync(
                Session.IdentityServer,
                refreshToken,
                cancellationToken);
        }

        return Session;
    }

    public async Task<Session> LoginAsync(
        string? authority,
        CancellationToken cancellationToken)
    {
        var client = CreateClient(x =>
            {
                if (string.IsNullOrWhiteSpace(authority))
                {
                    x.Authority = OidcConfiguration.IdentityUrl;
                }
                else if (authority.StartsWith("https://") || authority.StartsWith("http://"))
                {
                    x.Authority = authority;
                }
                else
                {
                    x.Authority = $"https://{authority}";
                }
            })
            .SetupBrowser();

        var result = await client.LoginAsync(new LoginRequest(), cancellationToken);

        result.EnsureNoError();

        Session = result.ToSession();

        await _configurationService.SaveAsync(Session, cancellationToken);

        return Session;
    }

    private async Task RefreshTokenAsync(
        string authority,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (Session is not { })
        {
            return;
        }

        RefreshTokenResult? result = null;
        try
        {
            result = await CreateClient(x => x.Authority = authority)
                .RefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
        }
        catch
        {
            // If the refresh token fails, we need to clear the session
        }

        if (result is not { IsError: false })
        {
            Session.Tokens = null;
        }
        else
        {
            Session.Tokens = new Tokens(
                result.AccessToken,
                result.IdentityToken,
                result.RefreshToken,
                result.AccessTokenExpiration);
        }

        await _configurationService.SaveAsync(Session, cancellationToken);
    }

    private async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        Session ??= await _configurationService.GetAsync<Session>(cancellationToken)
            ?? throw new ExitException(
                $"User session could not be loaded, run {"nitro login".AsCommand()} first.");
    }

    private OidcClient CreateClient(Action<OidcClientOptions>? configure = null)
    {
        var options = new OidcClientOptions
        {
            Authority = OidcConfiguration.IdentityUrl,
            ClientId = OidcConfiguration.ClientId,
            Scope = OidcConfiguration.Scopes,
            FilterClaims = false,
            LoadProfile = false,
            DisablePushedAuthorization = true
        };

        configure?.Invoke(options);

        return new DynamicAuthorityOidcClient(options);
    }
}

/// <summary>
/// This client allows to challenge a different IDP when there was a redirect on the authorize
/// endpoint
/// </summary>
file sealed class DynamicAuthorityOidcClient : OidcClient
{
    public DynamicAuthorityOidcClient(OidcClientOptions options) : base(options)
    {
    }

    public override Task<LoginResult> ProcessResponseAsync(
        string data,
        AuthorizeState state,
        Parameters? backChannelParameters = null,
        CancellationToken cancellationToken = default)
    {
        Options.ProviderInformation = null;
        Options.Authority = new AuthorizeResponse(data).Issuer;
        Options.Policy = new Policy();

        return base.ProcessResponseAsync(data, state, backChannelParameters, cancellationToken);
    }
}

file static class LocalExtensions
{
    public static OidcClient SetupBrowser(this OidcClient client)
    {
        var browser = new SystemBrowser();
        client.Options.RedirectUri = $"{browser.Host}/signin-redirect";
        client.Options.Browser = browser;
        return client;
    }

    public static void EnsureNoError(this LoginResult result)
    {
        if (result.IsError)
        {
            throw new ExitException($"{result.Error}\n{result.ErrorDescription}");
        }
    }

    public static Session ToSession(this LoginResult result)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        string? userId = null;
        string? sessionId = null;
        string? email = null;
        string? tenant = null;
        string? issuer = null;
        string? apiUrl = null;

        foreach (var claim in result.User.Claims.Concat(token.Claims))
        {
            switch (claim.Type)
            {
                case Issuer:
                    issuer = claim.Value;
                    break;

                case UserId:
                    userId = claim.Value;
                    break;

                case SessionId:
                    sessionId = claim.Value;
                    break;

                case Email:
                    email = claim.Value;
                    break;

                case Tenant:
                    tenant = claim.Value;
                    break;

                case ApiUrl:
                    apiUrl = claim.Value;
                    break;
            }
        }

        if (userId is null ||
            sessionId is null ||
            email is null ||
            tenant is null ||
            issuer is null ||
            apiUrl is null)
        {
            throw new ExitException("The session");
        }

        return new Session(
            sessionId,
            userId,
            tenant,
            issuer,
            apiUrl,
            email,
            new Tokens(
                result.AccessToken,
                result.IdentityToken,
                result.RefreshToken,
                result.AccessTokenExpiration),
            null);
    }
}
