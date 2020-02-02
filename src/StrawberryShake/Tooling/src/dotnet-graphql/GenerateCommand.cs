using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public static class GenerateCommand
    {
        public static CommandLineApplication Create()
        {
            var generate = new CommandLineApplication();
            generate.AddName("generate");
            generate.AddHelp<GenerateHelpTextGenerator>();

            CommandOption pathArg = generate.Option(
               "-p|--Path",
               "The directory where the client shall be located.",
               CommandOptionType.SingleValue);

            CommandOption languageArg = generate.Option(
                "-l|--LanguageVersion",
                "The C# Language Version (7.3 or 8.0).",
                CommandOptionType.SingleValue);

            CommandOption diSupportArg = generate.Option(
                "-d|--DISupport",
                "Generate dependency injection integration for " +
                "Microsoft.Extensions.DependencyInjection.",
                CommandOptionType.NoValue);

            CommandOption namespaceArg = generate.Option(
                "-n|--Namespace",
                "The namespace that shall be used for the generated files.",
                CommandOptionType.SingleValue);

            CommandOption searchArg = generate.Option(
                "-s|--search",
                "Search for client directories.",
                CommandOptionType.NoValue);

            CommandOption forceArg = generate.Option(
                "-f|--Force",
                "Force code generation even if nothing has changed.",
                CommandOptionType.NoValue);

            CommandOption jsonArg = generate.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            generate.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new GenerateCommandArguments(
                    pathArg, languageArg, diSupportArg, namespaceArg, searchArg, forceArg);
                var handler = CommandTools.CreateHandler<GenerateCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return generate;
        }
    }
}
