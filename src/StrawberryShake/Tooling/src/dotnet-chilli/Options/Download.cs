using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("download")]
    public class Download : AuthOptions
    {
        [Option('u', "uri", HelpText = "The URL of the GraphQL endpoint.", Required = true)]
        public string Uri { get; set; }

        [Option('f', "FileName", HelpText = "The file name to store the schema SDL.")]
        public string FileName { get; set; }

    }
}
