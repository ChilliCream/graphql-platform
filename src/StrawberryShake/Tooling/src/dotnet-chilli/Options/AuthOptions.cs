using CommandLine;

namespace StrawberryShake.Tools.Options
{
    public class AuthOptions : BaseOptions
    {
        public const string DefaultScheme = "bearer";

        [Option("token", HelpText = "The token that shall be used to authenticate with the GraphQL server.")]
        public string Token { get; set; }

        [Option("scheme", HelpText = "The token scheme (default: " + DefaultScheme + ")", Default = DefaultScheme)]
        public string Scheme { get; set; } = DefaultScheme;

        [Option("tokenEndpoint", HelpText = "The token endpoint uri.")]
        public string TokenEndpoint { get; set; }

        [Option("clientId", HelpText = "The client id.")]
        public string ClientId { get; set; }

        [Option("clientSecret", HelpText = "The client secret.")]
        public string ClientSecret { get; set; }

        [Option("scopes",  HelpText = "Custom scopes that shall be passed along with the token request.")]
        public string[] Scopes { get; set; }
    }
}
