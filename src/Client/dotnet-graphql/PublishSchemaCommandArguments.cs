using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandArguments
        : AuthArgumentsBase
    {
        public PublishSchemaCommandArguments(
            CommandOption registry,
            CommandOption schemaName,
            CommandOption environmentName,
            CommandOption schemaFileName,
            CommandOption tag,
            CommandOption token,
            CommandOption scheme,
            CommandOption tokenEndpoint,
            CommandOption clientId,
            CommandOption clientSecret,
            CommandOption scopes)
        {
            Registry = registry;
            SchemaName = schemaName;
            EnvironmentName = environmentName;
            SchemaFileName = schemaFileName;
            Tag = tag;
            Token = token;
            Scheme = scheme;
        }

        public CommandOption Registry { get; }
        public CommandOption SchemaName { get; }
        public CommandOption EnvironmentName { get; }
        public CommandOption SchemaFileName { get; }
        public CommandOption Tag { get; }
        public CommandOption Token { get; }
        public CommandOption Scheme { get; }
    }
}
