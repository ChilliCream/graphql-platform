using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("update")]
    public class Update : BaseOptions
    {
        public Update(bool json, string? path, string? uri) : base(json)
        {
            Path = path;
            Uri = uri;
        }

        [Option('p', "path", HelpText = "The directory where the client shall be located.")]
        public string? Path { get; }

        [Option('u', "uri", HelpText = "The URL to the GraphQL endpoint.")]
        public string? Uri { get; }

    }
}
