using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandArguments
    {
        public PublishSchemaCommandArguments(
            CommandOption registry,
            CommandOption schemaName,
            CommandOption environmentName,
            CommandOption schemaFileName,
            CommandOption tag,
            CommandOption token,
            CommandOption scheme)
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
