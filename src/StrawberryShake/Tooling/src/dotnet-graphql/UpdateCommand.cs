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

            AuthArguments authArguments = update.AddAuthArguments();

            update.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new UpdateCommandArguments(urlArg, pathArg, authArguments);
                UpdateCommandHandler handler = CommandTools.CreateHandler<UpdateCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });
        }
    }
}
