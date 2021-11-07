using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class UpdateCommand
    {
        public static void Build(CommandLineApplication update)
        {
            update.Description = "Update local schema";

            CommandOption pathArg = update.Option(
                "-p|--Path",
                "The directory where the client shall be located.",
                CommandOptionType.SingleValue);

            CommandOption urlArg = update.Option(
                "-u|--uri",
                "The URL to the GraphQL endpoint.",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = update.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            CommandOption headersArg = update.Option(
                "-x|--headers",
                "Custom headers used in request to Graph QL server. " +
                "Can be used mulitple times. Example: --headers key1=value1 --headers key2=value2",
                CommandOptionType.MultipleValue);

            AuthArguments authArguments = update.AddAuthArguments();

            update.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new UpdateCommandArguments(
                    urlArg,
                    pathArg,
                    authArguments,
                    headersArg);
                UpdateCommandHandler handler = CommandTools.CreateHandler<UpdateCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });
        }
    }
}
