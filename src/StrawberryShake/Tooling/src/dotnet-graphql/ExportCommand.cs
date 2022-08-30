using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.Tools.Configuration;
using static System.Environment;
using static StrawberryShake.Tools.GeneratorHelpers;

namespace StrawberryShake.Tools;

public static class ExportCommand
{
    public static void Build(CommandLineApplication generate)
    {
        generate.Description = "Exports Persisted Queries for Strawberry Shake Clients";

        var pathArg = generate.Argument(
            "path",
            "The project directory.");

        var razorArg = generate.Option(
            "-o|--outputPath",
            "Output Directory.",
            CommandOptionType.SingleValue);

        var relayFormatArg = generate.Option(
            "-r|--relayFormat",
            "Export Persisted Queries as Relay Format.",
            CommandOptionType.NoValue);

        var jsonArg = generate.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        generate.OnExecuteAsync(ct =>
        {
            var arguments = new ExportCommandArguments(
                pathArg.Value ?? CurrentDirectory,
                razorArg.Value()!,
                relayFormatArg.HasValue());
            var handler = CommandTools.CreateHandler<ExportCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, ct);
        });
    }

    private sealed class ExportCommandHandler : CommandHandler<ExportCommandArguments>
    {
        public ExportCommandHandler(IConsoleOutput output)
        {
            Output = output;
        }

        public IConsoleOutput Output { get; }

        public override async Task<int> ExecuteAsync(
            ExportCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            /*
            using var activity = Output.WriteActivity("Export Persisted Queries");

            if (string.IsNullOrEmpty(arguments.OutputPath))
            {
                activity.WriteError(new HotChocolate.Error(
                    "The Output Directory `-o` must be set!"));
            }

            var generator = new CSharpGeneratorClient(GetCodeGenServerLocation());
            var documents = GetDocuments(arguments.Path);
            var configFiles = GetConfigFiles(arguments.Path);

            foreach (var configFileName in configFiles)
            {
                var config = await LoadConfigAsync(configFileName);

                var persistedDir = configFiles.Length == 1
                    ? arguments.OutputPath
                    : Path.Combine(arguments.OutputPath, config.Extensions.StrawberryShake.Name);

                var request = new GeneratorRequest(
                    configFileName,
                    documents,
                    persistedQueryDirectory: persistedDir,
                    option: arguments.RelayFormat
                        ? RequestOptions.ExportPersistedQueriesJson
                        : RequestOptions.ExportPersistedQueries);

                var response = generator.Execute(request);

                if (response.TryLogErrors(activity))
                {
                    return 1;
                }
            }
            */

            return 0;
        }

        private static async Task<GraphQLConfig> LoadConfigAsync(string configFileName)
        {
            var json = await File.ReadAllTextAsync(configFileName);
            return GraphQLConfig.FromJson(json);
        }
    }

    private sealed class ExportCommandArguments
    {
        public ExportCommandArguments(string path, string outputPath, bool relayFormat)
        {
            Path = path;
            OutputPath = outputPath;
            RelayFormat = relayFormat;
        }

        public string Path { get; }

        public string OutputPath { get; }

        public bool RelayFormat { get; }
    }
}
