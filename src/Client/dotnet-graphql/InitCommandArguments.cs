using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandArguments
        : AuthArgumentsBase
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption schema,
            CommandOption token,
            CommandOption scheme,
            CommandOption tokenEndpoint,
            CommandOption clientId,
            CommandOption clientSecret,
            CommandOption scopes)
            : base(token, scheme, tokenEndpoint, clientId, clientSecret, scopes)
        {
            Uri = uri;
            Path = path;
            Schema = schema;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Schema { get; }
    }
}
