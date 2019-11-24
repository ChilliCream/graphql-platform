using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class CompileCommand
    {
        public static CommandLineApplication Create()
        {
            var generate = new CommandLineApplication();
            generate.AddName("compile");
            generate.AddHelp<CompileHelpTextGenerator>();

            CommandOption pathArg = generate.Option(
                "-p|--Path",
                "The directory where the client shall be located.",
                CommandOptionType.SingleValue);

            CommandOption searchArg = generate.Option(
                "-s|--search",
                "Search for client directories.",
                CommandOptionType.NoValue);

            CommandOption jsonArg = generate.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            generate.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new CompileCommandArguments(pathArg, searchArg);
                var handler = CommandTools.CreateHandler<CompileCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return generate;
        }
    }
}
