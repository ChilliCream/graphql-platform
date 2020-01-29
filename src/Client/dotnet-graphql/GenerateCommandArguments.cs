using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class GenerateCommandArguments
    {
        public GenerateCommandArguments(
            CommandOption path,
            CommandOption language,
            CommandOption diSupport,
            CommandOption @namespace,
            CommandOption search,
            CommandOption force)
        {
            Path = path;
            Language = language;
            DISupport = diSupport;
            Namespace = @namespace;
            Search = search;
            Force = force;
        }

        public CommandOption Path { get; }
        public CommandOption Language { get; }
        public CommandOption DISupport { get; }
        public CommandOption Namespace { get; }
        public CommandOption Search { get; }
        public CommandOption Force { get; }
    }
}
