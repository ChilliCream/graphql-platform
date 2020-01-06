using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public static class DownloadCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("download");
            init.AddHelp<InitHelpTextGenerator>();

            CommandArgument uriArg = init.Argument(
                "uri",
                "The URL to the GraphQL endpoint.",
                c => c.IsRequired());

            CommandOption fileNameArg = init.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption tokenArg = init.Option(
                "-t|--token",
                "The token that shall be used to autheticate with the GraphQL server.",
                CommandOptionType.SingleValue);

            CommandOption schemeArg = init.Option(
                "-s|--scheme",
                "The token scheme (default: bearer).",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = init.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            init.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new DownloadCommandArguments(
                    uriArg, fileNameArg, tokenArg, schemeArg);
                var handler = CommandTools.CreateHandler<DownloadCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return init;
        }
    }
}
