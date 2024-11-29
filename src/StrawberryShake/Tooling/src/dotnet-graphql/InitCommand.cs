using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public static class InitCommand
{
    public static void Build(CommandLineApplication init)
    {
        init.Description = "Initialize project and download schema";

        var uriArg = init.Argument(
            "uri",
            "The URL to the GraphQL endpoint.",
            c => c.IsRequired());

        var pathArg = init.Option(
            "-p|--Path",
            "The directory where the client shall be located.",
            CommandOptionType.SingleValue);

        var nameArg = init.Option(
            "-n|--clientName",
            "The GraphQL client name.",
            CommandOptionType.SingleValue);

        var jsonArg = init.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        var headersArg = init.Option(
            "-x|--headers",
            "Custom headers used in request to Graph QL server. " +
            "Can be used multiple times. Example: --headers key1=value1 --headers key2=value2",
            CommandOptionType.MultipleValue);

        var fromFileArg = init.Option(
            "-f|--FromFile",
            "Import schema from schema file.",
            CommandOptionType.NoValue);

        var depthArg = init.Option(
            "-d|--typeDepth",
            "The type depth used for the introspection request.",
            CommandOptionType.SingleOrNoValue);

        var authArguments = init.AddAuthArguments();

        init.OnExecuteAsync(cancellationToken =>
        {
            var arguments = new InitCommandArguments(
                uriArg,
                pathArg,
                nameArg,
                authArguments,
                headersArg,
                fromFileArg,
                depthArg);
            var handler = CommandTools.CreateHandler<InitCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, cancellationToken);
        });
    }
}
