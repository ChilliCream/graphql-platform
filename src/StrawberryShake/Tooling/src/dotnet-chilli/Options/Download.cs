using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("download")]
    public class Download : AuthOptions
    {
        public Download(bool json, string? token, string? scheme, string? tokenEndpoint, string? clientId, string? clientSecret, string[]? scopes, string uri, string? fileName) : base(json, token, scheme, tokenEndpoint, clientId, clientSecret, scopes)
        {
            Uri = uri;
            FileName = fileName;
        }

        [Option('u', "uri", HelpText = "The URL of the GraphQL endpoint.", Required = true)]
        public string Uri { get; }

        [Option('f', "FileName", HelpText = "The file name to store the schema SDL.")]
        public string? FileName { get; }

    }
}
