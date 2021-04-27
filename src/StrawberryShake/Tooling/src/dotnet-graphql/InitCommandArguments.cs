using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandArguments
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption name,
            AuthArguments authArguments)
        {
            Uri = uri;
            Path = path;
            Name = name;
            AuthArguments = authArguments;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Name { get; }
        public AuthArguments AuthArguments { get; }
    }
}
