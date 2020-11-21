using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("compile")]
    public class Compile : BaseOptions
    {
        public Compile(bool json, string path, string search) : base(json)
        {
            Path = path;
            Search = search;
        }

        [Option('p', "path", HelpText = "The directory where the client shall be located.")]
        public string Path { get; }

        [Option('s', "search", HelpText = "Search for client directories.")]
        public string Search { get; }

    }
}
