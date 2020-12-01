using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandArguments
    {
        public PublishSchemaCommandArguments(
            CommandArgument registry,
            CommandArgument externalId,
            CommandArgument environmentName,
            CommandArgument schemaName,
            CommandOption schemaFileName,
            CommandOption tag,
            CommandOption published,
            AuthArguments authArguments)
        {
            Registry = registry;
            ExternalId = externalId;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            SchemaFileName = schemaFileName;
            Tag = tag;
            Published = published;
            AuthArguments = authArguments;
        }

        public CommandArgument Registry { get; }

        public CommandArgument ExternalId { get; }

        public CommandArgument EnvironmentName { get; }

        public CommandArgument SchemaName { get; }

        public CommandOption SchemaFileName { get; }

        public CommandOption Tag { get; }

        public CommandOption Published { get; }

        public AuthArguments AuthArguments { get; }
    }
}
