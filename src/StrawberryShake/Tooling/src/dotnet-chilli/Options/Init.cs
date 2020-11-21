using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("init")]
    public class Init : BaseOptions
    {
        [Option('u', "uri", HelpText = "The URL to the GraphQL endpoint.", Required = true)]
        public string Uri { get; }

        [Option('p', "path", HelpText =  "The directory where the client shall be located.")]
        public string? Path { get; }

        [Option('s', "schemaName", HelpText = "The schema name.")]
        public string? SchemaName { get; }

        public Init(bool json, string uri, string? path, string? schemaName) : base(json)
        {
            Uri = uri;
            Path = path;
            SchemaName = schemaName;
        }

    }
}
