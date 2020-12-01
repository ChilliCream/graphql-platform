using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class PublishClientCommandArguments
    {
        public PublishClientCommandArguments(
            CommandArgument registry,
            CommandArgument externalId,
            CommandArgument environmentName,
            CommandArgument schemaName,
            CommandArgument clientName,
            CommandOption searchDirectory,
            CommandOption queryFileName,
            CommandOption relayFileFormat,
            CommandOption tag,
            CommandOption published,
            AuthArguments authArguments)
        {
            Registry = registry;
            ExternalId = externalId;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ClientName = clientName;
            SearchDirectory = searchDirectory;
            QueryFileName = queryFileName;
            RelayFileFormat = relayFileFormat;
            Tag = tag;
            Published = published;
            AuthArguments = authArguments;
        }

        public CommandArgument Registry { get; }

        public CommandArgument ExternalId { get; }

        public CommandArgument EnvironmentName { get; }

        public CommandArgument SchemaName { get; }

        public CommandArgument ClientName { get; }

        public CommandOption SearchDirectory { get; }

        public CommandOption QueryFileName { get; }

        public CommandOption RelayFileFormat { get; }

        public CommandOption Tag { get; }

        public CommandOption Published { get; }

        public AuthArguments AuthArguments { get; }
    }
}
