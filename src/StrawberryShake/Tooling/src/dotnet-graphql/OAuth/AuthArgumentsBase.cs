using McMaster.Extensions.CommandLineUtils;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools.OAuth;

public sealed class AuthArguments
{
    private const string _defaultScheme = "bearer";

    public AuthArguments(
        CommandOption token,
        CommandOption scheme,
        CommandOption tokenEndpoint,
        CommandOption clientId,
        CommandOption clientSecret,
        CommandOption scopes,
        CommandOption noScheme)
    {
        Token = token;
        Scheme = scheme;
        TokenEndpoint = tokenEndpoint;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Scopes = scopes;
        NoScheme = noScheme;
    }

    public CommandOption Token { get; }
    public CommandOption Scheme { get; }
    public CommandOption NoScheme { get; }
    public CommandOption TokenEndpoint { get; }
    public CommandOption ClientId { get; }
    public CommandOption ClientSecret { get; }
    public CommandOption Scopes { get; }

    public async Task<AccessToken?> RequestTokenAsync(
        IConsoleOutput output,
        CancellationToken cancellationToken)
    {
        string? scheme = null;

        if (!NoScheme.HasValue())
        {
            scheme = Scheme.HasValue() ? Scheme.Value()!.Trim() : _defaultScheme;
        }

        if (Token.HasValue())
        {
            return new AccessToken(
                Token.Value()!.Trim(),
                scheme);
        }

        if (TokenEndpoint.HasValue() || ClientId.HasValue() || ClientSecret.HasValue())
        {
            using var activity = output.WriteActivity("Request token");
            ValidateOAuthArguments(activity);
            var scopes = Scopes.HasValue()
                ? Scopes.Values.Where(t => t is { }).OfType<string>()
                : [];
            var token = await TokenClient.GetTokenAsync(
                    TokenEndpoint.Value()!.Trim(),
                    ClientId.Value()!.Trim(),
                    ClientSecret.Value()!.Trim(),
                    scopes,
                    cancellationToken)
                .ConfigureAwait(false);
            return new AccessToken(token, scheme);
        }

        return null;
    }

    private void ValidateOAuthArguments(IActivity activity)
    {
        if (!TokenEndpoint.HasValue())
        {
            activity.WriteError(
                HCErrorBuilder.New()
                    .SetMessage("TokenEndpoint has to be set.")
                    .SetCode("ARGUMENT_MISSING")
                    .Build());
        }

        if (!ClientId.HasValue())
        {
            activity.WriteError(
                HCErrorBuilder.New()
                    .SetMessage("ClientId has to be set.")
                    .SetCode("ARGUMENT_MISSING")
                    .Build());
        }

        if (!ClientSecret.HasValue())
        {
            activity.WriteError(
                HCErrorBuilder.New()
                    .SetMessage("ClientSecret has to be set.")
                    .SetCode("ARGUMENT_MISSING")
                    .Build());
        }
    }
}
