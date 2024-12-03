using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools.OAuth;

public static class CommandLineApplicationExtensions
{
    public static AuthArguments AddAuthArguments(
        this CommandLineApplication app)
    {
        var tokenEndpoint = app.Option(
            "--tokenEndpoint",
            "The token endpoint uri.",
            CommandOptionType.SingleValue);

        var clientId = app.Option(
            "--clientId",
            "The client id.",
            CommandOptionType.SingleValue,
            c => c.ShortName = null);

        var clientSecret = app.Option(
            "--clientSecret",
            "The client secret.",
            CommandOptionType.SingleValue);

        var scopes = app.Option(
            "--scope",
            "A custom scope that shall be passed along with the token request.",
            CommandOptionType.MultipleValue);

        var token = app.Option(
            "--token",
            "The token that shall be used to authenticate with the GraphQL server.",
            CommandOptionType.SingleValue);

        var scheme = app.Option(
            "--scheme",
            "The token scheme (default: bearer).",
            CommandOptionType.SingleValue);

        var noScheme = app.Option(
            "--no-scheme",
            "The token will be send without a scheme.",
            CommandOptionType.NoValue);

        return new AuthArguments(
            token,
            scheme,
            tokenEndpoint,
            clientId,
            clientSecret,
            scopes,
            noScheme);
    }
}
