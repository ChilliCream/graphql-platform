using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandArguments
    {
        public PublishSchemaCommandArguments(
            CommandArgument registry,
            CommandArgument environmentName,
            CommandArgument schemaName,
            CommandArgument schemaFileName,
            CommandOption tag,
            AuthArguments authArguments)
        {
            Registry = registry;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            SchemaFileName = schemaFileName;
            Tag = tag;
            AuthArguments = authArguments;
        }

        public CommandArgument Registry { get; }
        public CommandArgument EnvironmentName { get; }
        public CommandArgument SchemaName { get; }
        public CommandArgument SchemaFileName { get; }
        public CommandOption Tag { get; }
        public AuthArguments AuthArguments { get; }
    }
}
