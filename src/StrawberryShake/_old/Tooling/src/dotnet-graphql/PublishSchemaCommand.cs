using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{

    public static class PublishSchemaCommand
    {
        public static CommandLineApplication Create()
        {
            var publish = new CommandLineApplication();
            publish.AddName("schema");
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

            CommandArgument externalId = publish.Argument(
                "externalId",
                "An external identifier to track the schema through the publish process.",
                c => c.IsRequired());

            CommandOption schemaFileNameArg = publish.Option(
                "-f|--schemaFileName",
                "schemaFileName",
                CommandOptionType.SingleValue);

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
                var arguments = new PublishSchemaCommandArguments(
                    registryArg,
                    externalId,
                    environmentNameArg,
                    schemaNameArg,
                    schemaFileNameArg,
                    tagArg,
                    publishedArg,
                    authArguments);
                var handler = CommandTools.CreateHandler<PublishSchemaCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return publish;
        }
    }
}
