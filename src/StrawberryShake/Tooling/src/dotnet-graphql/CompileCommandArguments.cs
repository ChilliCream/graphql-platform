using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class CompileCommandArguments
    {
        public CompileCommandArguments(
            CommandOption path,
            CommandOption search)
        {
            Path = path;
            Search = search;
        }

        public CommandOption Path { get; }
        public CommandOption Search { get; }
    }
}
