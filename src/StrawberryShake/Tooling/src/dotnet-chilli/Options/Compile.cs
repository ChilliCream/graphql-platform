using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("compile")]
    public class Compile : BaseOptions
    {
        [Option('p', "path", HelpText = "The directory where the client shall be located.")]
        public string Path { get; set; }

        [Option('s', "search", HelpText = "Search for client directories.")]
        public string Search { get; set; }

    }
}
