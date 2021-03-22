using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public static class InitCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("init");
            init.AddHelp<InitHelpTextGenerator>();

            CommandArgument uriArg = init.Argument(
                "uri",
                "The URL to the GraphQL endpoint.",
                c => c.IsRequired());

            CommandOption pathArg = init.Option(
                "-p|--Path",
                "The directory where the client shall be located.",
                CommandOptionType.SingleValue);

            CommandOption nameArg = init.Option(
                "-n|--clientName",
                "The GraphQL client name.",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = init.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            AuthArguments authArguments = init.AddAuthArguments();

            init.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new InitCommandArguments(
                    uriArg,
                    pathArg,
                    nameArg,
                    authArguments);
                InitCommandHandler handler =
                    CommandTools.CreateHandler<InitCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return init;
        }
    }
}
