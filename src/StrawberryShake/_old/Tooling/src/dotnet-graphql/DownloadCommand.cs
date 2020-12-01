using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class DownloadCommand
    {
        public static CommandLineApplication Create()
        {
            var download = new CommandLineApplication();
            download.AddName("download");
            download.AddHelp<InitHelpTextGenerator>();

            CommandArgument uriArg = download.Argument(
                "uri",
                "The URL to the GraphQL endpoint.",
                c => c.IsRequired());

            CommandOption fileNameArg = download.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = download.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            AuthArguments authArguments = download.AddAuthArguments();

            download.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new DownloadCommandArguments(
                    uriArg,
                    fileNameArg,
                    authArguments);
                var handler = CommandTools.CreateHandler<DownloadCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return download;
        }
    }
}
