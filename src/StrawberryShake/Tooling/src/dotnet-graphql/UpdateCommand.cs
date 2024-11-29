using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public static class UpdateCommand
{
    public static void Build(CommandLineApplication update)
    {
        update.Description = "Update local schema";

        var pathArg = update.Option(
            "-p|--Path",
            "The directory where the client shall be located.",
            CommandOptionType.SingleValue);

        var urlArg = update.Option(
            "-u|--uri",
            "The URL to the GraphQL endpoint.",
            CommandOptionType.SingleValue);

        var jsonArg = update.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        var headersArg = update.Option(
            "-x|--headers",
            "Custom headers used in request to Graph QL server. " +
            "Can be used multiple times. Example: --headers key1=value1 --headers key2=value2",
            CommandOptionType.MultipleValue);

        var depthArg = update.Option(
            "-d|--typeDepth",
            "The type depth used for the introspection request.",
            CommandOptionType.SingleOrNoValue);

        var authArguments = update.AddAuthArguments();

        update.OnExecuteAsync(cancellationToken =>
        {
            var arguments = new UpdateCommandArguments(urlArg, pathArg, authArguments, headersArg, depthArg);
            var handler = CommandTools.CreateHandler<UpdateCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, cancellationToken);
        });
    }
}
