using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class InitCommandArguments
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption schema,
            CommandOption token,
            CommandOption scheme)
        {
            Uri = uri;
            Path = path;
            Schema = schema;
            Token = token;
            Scheme = scheme;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Schema { get; }
        public CommandOption Token { get; }
        public CommandOption Scheme { get; }
    }
}
