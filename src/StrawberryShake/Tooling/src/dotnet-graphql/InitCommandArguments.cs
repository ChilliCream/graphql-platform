using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandArguments
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption schema,
            AuthArguments authArguments)
        {
            Uri = uri;
            Path = path;
            Schema = schema;
            AuthArguments = authArguments;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Schema { get; }
        public AuthArguments AuthArguments { get; }
    }
}
