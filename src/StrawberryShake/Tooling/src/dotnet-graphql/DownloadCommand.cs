using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class DownloadCommand
    {
        public static void Build(CommandLineApplication download)
        {
            download.Description = "Download the schema as GraphQL SDL";

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
                DownloadCommandHandler handler =
                    CommandTools.CreateHandler<DownloadCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });
        }
    }
}
