using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{

    public static class PublishClientCommand
    {
        public static CommandLineApplication Create()
        {
            var publish = new CommandLineApplication();
            publish.AddName("client");
            //publish.AddHelp<InitHelpTextGenerator>();

            CommandArgument registryArg = publish.Argument(
                "registry",
                "The URL to the GraphQL schema registry.",
                c => c.IsRequired());

            CommandArgument environmentNameArg = publish.Argument(
                "environmentName",
                "The name of the environment.",
                c => c.IsRequired());

            CommandArgument schemaNameArg = publish.Argument(
                "schemaName",
                "The name of the schema.",
                c => c.IsRequired());

            CommandArgument clientNameArg = publish.Argument(
                "clientName",
                "The name of the client.",
                c => c.IsRequired());

            CommandArgument externalId = publish.Argument(
                "externalId",
                "An external identifier to track the schema through the publish process.",
                c => c.IsRequired());

            CommandOption searchDirectoryArg = publish.Option(
                "-d|--searchDirectory",
                "Files containing queries.",
                CommandOptionType.MultipleValue);

            CommandOption queryFileNameArg = publish.Option(
                "-f|--queryFileName",
                "Files containing queries.",
                CommandOptionType.MultipleValue);

            CommandOption relayFileFormatArg = publish.Option(
                "-r|--relayFileFormat",
                "Defines that the files are in the relay persisted query file format.",
                CommandOptionType.NoValue);

            CommandOption tagArg = publish.Option(
                "-t|--tag",
                "A custom tag that can be passed to the schema registry.",
                CommandOptionType.MultipleValue);

            CommandOption publishedArg = publish.Option(
                "-p|--published",
                "A custom tag that can be passed to the schema registry.",
                CommandOptionType.NoValue);

            CommandOption jsonArg = publish.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            AuthArguments authArguments = publish.AddAuthArguments();

            publish.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new PublishClientCommandArguments(
                    registryArg,
                    externalId,
                    environmentNameArg,
                    schemaNameArg,
                    clientNameArg,
                    searchDirectoryArg,
                    queryFileNameArg,
                    relayFileFormatArg,
                    tagArg,
                    publishedArg,
                    authArguments);
                var handler = CommandTools.CreateHandler<PublishClientCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return publish;
        }
    }
}
