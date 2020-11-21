using CommandLine;

namespace StrawberryShake.Tools.Options
{
    public class AuthOptions : BaseOptions
    {
        public const string DefaultScheme = "bearer";

        [Option("token", HelpText = "The token that shall be used to authenticate with the GraphQL server.")]
        public string? Token { get; }

        [Option("scheme", HelpText = "The token scheme (default: " + DefaultScheme + ")", Default = DefaultScheme)]
        public string? Scheme { get; }

        [Option("tokenEndpoint", HelpText = "The token endpoint uri.")]
        public string? TokenEndpoint { get; }

        [Option("clientId", HelpText = "The client id.")]
        public string? ClientId { get; }

        [Option("clientSecret", HelpText = "The client secret.")]
        public string? ClientSecret { get; }

        [Option("scopes",  HelpText = "Custom scopes that shall be passed along with the token request.")]
        public string[]? Scopes { get; }

        public AuthOptions(bool json, string? token, string? scheme, string? tokenEndpoint, string? clientId, string? clientSecret, string[]? scopes) : base(json)
        {
            Token = token;
            Scheme = scheme;
            TokenEndpoint = tokenEndpoint;
            ClientId = clientId;
            ClientSecret = clientSecret;
            Scopes = scopes;
        }
    }
}
