using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public static class DownloadCommand
{
    public static void Build(CommandLineApplication download)
    {
        download.Description = "Download the schema as GraphQL SDL";

        var uriArg = download.Argument(
            "uri",
            "The URL to the GraphQL endpoint.",
            c => c.IsRequired());

        var fileNameArg = download.Option(
            "-f|--FileName",
            "The file name to store the schema SDL.",
            CommandOptionType.SingleValue);

        var jsonArg = download.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        var headersArg = download.Option(
            "-x|--headers",
            "Custom headers used in request to Graph QL server. " +
            "Can be used multiple times. Example: --headers key1=value1 --headers key2=value2",
            CommandOptionType.MultipleValue);

        var depthArg = download.Option(
            "-d|--typeDepth",
            "The type depth used for the introspection request.",
            CommandOptionType.SingleOrNoValue);

        var authArguments = download.AddAuthArguments();

        download.OnExecuteAsync(cancellationToken =>
        {
            var arguments = new DownloadCommandArguments(uriArg, fileNameArg, authArguments, headersArg, depthArg);
            var handler = CommandTools.CreateHandler<DownloadCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, cancellationToken);
        });
    }
}
