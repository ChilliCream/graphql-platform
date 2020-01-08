using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class PublishSchemaCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("download");
            init.AddHelp<InitHelpTextGenerator>();

            CommandArgument registryArg = init.Argument(
                "registry",
                "The URL to the GraphQL schema registry.",
                c => c.IsRequired());

            CommandOption fileNameArg = init.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption fileNameArg = init.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption fileNameArg = init.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = init.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            AuthArguments arguments = init.AddAuthArguments();

            init.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new PublishSchemaCommandArguments(
                    registryArg,
                    fileNameArg,
                    authArguments);
                var handler = CommandTools.CreateHandler<PublishSchemaCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return init;
        }
    }
}
