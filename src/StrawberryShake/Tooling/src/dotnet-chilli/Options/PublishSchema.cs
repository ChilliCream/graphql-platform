using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("publish-schema")]
    public class PublishSchema : AuthOptions
    {
        public PublishSchema(bool json, string? token, string? scheme, string? tokenEndpoint, string? clientId, string? clientSecret, string[]? scopes, string registry, string environmentName, string schemaName, string externalId, string? schemaFileName, string[]? tags, string? published) : base(json, token, scheme, tokenEndpoint, clientId, clientSecret, scopes)
        {
            Registry = registry;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ExternalId = externalId;
            SchemaFileName = schemaFileName;
            Tags = tags;
            Published = published;
        }

        [Option('r', "registry", HelpText = "The URL to the GraphQL Schema registry", Required = true)]
        public string Registry { get; }

        [Option('e', "environmentName", HelpText = "The name of the environment", Required = true)]
        public string EnvironmentName { get; }

        [Option('s', "schemaName", HelpText = "The name of the schema", Required = true)]
        public string SchemaName { get; }

        [Option('i', "externalId", HelpText = "An external identifier to track the schema through the publish process.", Required = true)]
        public string ExternalId { get; }

        [Option('f', "schemaFileName", HelpText = "The schema file name.")]
        public string? SchemaFileName { get; }

        [Option('t', "tags", HelpText = "Custom tags that can be passed to the schema registry")]
        public string[]? Tags { get; }

        [Option('p', "published", HelpText = "A custom tag that can be passed to the schema registry.")]
        public string? Published { get; }

    }
}
