using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class UpdateCommandArguments
    {
        public UpdateCommandArguments(
            CommandOption uri,
            CommandOption path,
            CommandOption token,
            CommandOption scheme)
        {
            Uri = uri;
            Path = path;
            Token = token;
            Scheme = scheme;
        }

        public CommandOption Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Token { get; }
        public CommandOption Scheme { get; }
    }
}
