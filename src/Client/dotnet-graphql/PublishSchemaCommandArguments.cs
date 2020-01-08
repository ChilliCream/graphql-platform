using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

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
            AuthArguments authArguments)
        {
            Registry = registry;
            SchemaName = schemaName;
            EnvironmentName = environmentName;
            SchemaFileName = schemaFileName;
            Tag = tag;
            AuthArguments = authArguments;
        }

        public CommandOption Registry { get; }
        public CommandOption SchemaName { get; }
        public CommandOption EnvironmentName { get; }
        public CommandOption SchemaFileName { get; }
        public CommandOption Tag { get; }
        public AuthArguments AuthArguments { get; }
    }
}
