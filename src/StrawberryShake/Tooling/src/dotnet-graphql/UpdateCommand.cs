using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class UpdateCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("update");
            init.AddHelp<UpdateHelpTextGenerator>();

            CommandOption pathArg = init.Option(
                "-p|--Path",
                "The directory where the client shall be located.",
                CommandOptionType.SingleValue);

            CommandOption urlArg = init.Option(
                "-u|--uri",
                "The URL to the GraphQL endpoint.",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = init.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            AuthArguments authArguments = init.AddAuthArguments();

            init.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new UpdateCommandArguments(urlArg, pathArg, authArguments);
                var handler = CommandTools.CreateHandler<UpdateCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return init;
        }
    }
}
