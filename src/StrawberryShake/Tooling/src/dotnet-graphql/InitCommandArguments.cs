using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandArguments
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption Name,
            AuthArguments authArguments)
        {
            Uri = uri;
            Path = path;
            this.Name = Name;
            AuthArguments = authArguments;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Name { get; }
        public AuthArguments AuthArguments { get; }
    }
}
