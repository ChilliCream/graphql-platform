using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.CodeGeneration.CSharp.Analyzers;
using static System.Environment;
using static StrawberryShake.Tools.GeneratorHelpers;

namespace StrawberryShake.Tools;

public static class GenerateCommand
{
    public static void Build(CommandLineApplication generate)
    {
        generate.Description = "Generates Strawberry Shake Clients";

        CommandArgument pathArg = generate.Argument(
            "path",
            "The project directory.");

        CommandOption razorArg = generate.Option(
            "-r|--razor",
            "Generate Razor Components",
            CommandOptionType.NoValue);

        CommandOption jsonArg = generate.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        generate.OnExecuteAsync(ct =>
        {
            var arguments = new GenerateCommandArguments(
                pathArg.Value ?? CurrentDirectory,
                razorArg.HasValue());
            var handler = CommandTools.CreateHandler<GenerateCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, ct);
        });
    }

    private sealed class GenerateCommandHandler : CommandHandler<GenerateCommandArguments>
    {
        public GenerateCommandHandler(IConsoleOutput output)
        {
            Output = output;
        }

        public IConsoleOutput Output { get; }

        public override Task<int> ExecuteAsync(
            GenerateCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using var activity = Output.WriteActivity(
                arguments.RazorOnly
                    ? "Generate Razor Components"
                    : "Generate C# Clients");

            var generator = new CSharpGeneratorClient(GetCodeGenServerLocation());
            var documents = GetDocuments(arguments.Path);

            foreach (var configFileName in GetConfigFiles(arguments.Path))
            {
                var request = new GeneratorRequest(
                    configFileName,
                    documents,
                    option: arguments.RazorOnly
                        ? RequestOptions.GenerateRazorComponent
                        : RequestOptions.GenerateCSharpClient);

                var response = generator.Execute(request);

                if (response.TryLogErrors(activity))
                {
                    return Task.FromResult(1);
                }
            }

            return Task.FromResult(0);
        }
    }

    private sealed class GenerateCommandArguments
    {
        public GenerateCommandArguments(string path, bool razorOnly)
        {
            Path = path;
            RazorOnly = razorOnly;
        }

        public string Path { get; }

        public bool RazorOnly { get; }
    }
}
