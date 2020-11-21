using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("publish-client")]
    public class PublishClient : AuthOptions
    {
        public PublishClient(bool json, string? token, string? scheme, string? tokenEndpoint, string? clientId, string? clientSecret, string[]? scopes, string registry, string environmentName, string schemaName, string clientName, string externalId, string[]? searchDirectories, string[]? queryFileNames, string? relayFileFormat, string[]? tags, string? published) : base(json, token, scheme, tokenEndpoint, clientId, clientSecret, scopes)
        {
            Registry = registry;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ClientName = clientName;
            ExternalId = externalId;
            SearchDirectories = searchDirectories;
            QueryFileNames = queryFileNames;
            RelayFileFormat = relayFileFormat;
            Tags = tags;
            Published = published;
        }

        [Option('r', "registry", HelpText = "The URL to the GraphQL Schema registry", Required = true)]
        public string Registry { get; }

        [Option('e', "environmentName", HelpText = "The name of the environment", Required = true)]
        public string EnvironmentName { get; }

        [Option('s', "schemaName", HelpText = "The name of the schema", Required = true)]
        public string SchemaName { get; }

        [Option('c', "clietnName", HelpText = "The name of the client", Required = true)]
        public string ClientName { get; }

        [Option('i', "externalId", HelpText = "An external identifier to track the schema through the publish process.", Required = true)]
        public string ExternalId { get; }

        [Option('d', "searchDirectories", HelpText = "Files containing queries.")]
        public string[]? SearchDirectories { get; }

        [Option('f', "queryFileNames", HelpText = "Files containing queries.")]
        public string[]? QueryFileNames { get; }

        [Option('R', "relayFileFormat", HelpText = "Defines that the files are in the relay persisted query file format.")]
        public string? RelayFileFormat { get; }

        [Option('t', "tags", HelpText = "Custom tags that can be passed to the schema registry")]
        public string[]? Tags { get; }

        [Option('p', "published", HelpText = "A custom tag that can be passed to the schema registry.")]
        public string? Published { get; }


    }
}
