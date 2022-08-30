using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.Tools.Configuration;
using static System.Environment;
using static StrawberryShake.Tools.GeneratorHelpers;

namespace StrawberryShake.Tools;

public static class GenerateCommand
{
    public static void Build(CommandLineApplication generate)
    {
        generate.Description = "Generates Strawberry Shake Clients";

        var pathArg = generate.Argument(
            "path",
            "The project directory.");

        var rootNamespaceArg = generate.Option(
            "-n|--rootNamespace",
            "The root namespace.",
            CommandOptionType.SingleValue);

        var disableSchemaValidationArg = generate.Option(
            "-s|--disableSchemaValidation",
            "Disable strict schema validation.",
            CommandOptionType.NoValue);

        var hashAlgorithmArg = generate.Option(
            "-a|--hashAlgorithm",
            "Request Hash Generation.",
            CommandOptionType.SingleValue);

        var disableStoreArg = generate.Option(
            "-t|--disableStore",
            "Disable the client store.",
            CommandOptionType.NoValue);

        var razorComponentsArg = generate.Option(
            "-r|--razorComponents",
            "Generate Razor components.",
            CommandOptionType.NoValue);

        var outputDirArg = generate.Option(
            "-o|--outputDirectory",
            "The output directory.",
            CommandOptionType.SingleValue);

        var jsonArg = generate.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        generate.OnExecuteAsync(ct =>
        {
            var arguments = new GenerateCommandArguments(
                pathArg.Value ?? CurrentDirectory,
                rootNamespaceArg.Value(),
                !disableSchemaValidationArg.HasValue(),
                hashAlgorithmArg.Value() ?? "md5",
                true,
                disableStoreArg.HasValue(),
                razorComponentsArg.HasValue(),
                outputDirArg.Value());
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

        private IConsoleOutput Output { get; }

        public override async Task<int> ExecuteAsync(
            GenerateCommandArguments args,
            CancellationToken cancellationToken)
        {
            using var activity = Output.WriteActivity("Generate C# Clients");
            var statusCode = 0;

            foreach (var configFileName in GetConfigFiles(args.Path))
            {
                var configBody = await File.ReadAllTextAsync(configFileName, cancellationToken);
                var config = GraphQLConfig.FromJson(configBody);
                var clientName = config.Extensions.StrawberryShake.Name;
                var rootNamespace = args.RootNamespace ?? $"{clientName}NS";
                var documents = GetGraphQLDocuments(args.Path, config.Documents);
                var settings = CreateSettings(config, args, rootNamespace);
                var result = CSharpGenerator.Generate(documents, settings);
                var outputDir = args.OutputDir ?? Path.Combine(
                    Path.GetDirectoryName(configFileName)!,
                    config.Extensions.StrawberryShake.OutputDirectoryName);

                if (result.HasErrors())
                {
                    statusCode = 1;
                    activity.WriteErrors(result.Errors);
                }
                else
                {
                    foreach (var doc in result.Documents)
                    {
                        if (doc.Kind is SourceDocumentKind.CSharp or SourceDocumentKind.Razor)
                        {
                            var fileName = doc.Path is null
                                ? Path.Combine(outputDir, doc.Name + ".cs")
                                : Path.Combine(outputDir, doc.Path, doc.Name + ".cs");
                            var dir = Path.GetDirectoryName(fileName)!;

                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            if (File.Exists(fileName))
                            {
                                File.Delete(fileName);
                            }

                            await File.WriteAllTextAsync(
                                fileName,
                                doc.SourceText,
                                cancellationToken);

                            Output.WriteFileCreated(fileName);
                        }
                    }
                }

            }

            return statusCode;
        }
    }

    internal sealed class GenerateCommandArguments
    {
        public GenerateCommandArguments(
            string path,
            string? rootNamespace,
            bool strictSchemaValidation,
            string hashAlgorithm,
            bool useSingleFile,
            bool noStore,
            bool razorComponents,
            string? outputDir)
        {
            Path = path;
            RootNamespace = rootNamespace;
            StrictSchemaValidation = strictSchemaValidation;
            HashAlgorithm = hashAlgorithm;
            UseSingleFile = useSingleFile;
            NoStore = noStore;
            RazorComponents = razorComponents;
            OutputDir = outputDir;
        }

        public string Path { get; }

        public string? RootNamespace { get; }

        public bool StrictSchemaValidation { get; }

        public string HashAlgorithm { get; }

        public bool UseSingleFile { get; }

        public bool NoStore { get; }

        public bool RazorComponents { get; }

        public string? OutputDir { get; }
    }
}
