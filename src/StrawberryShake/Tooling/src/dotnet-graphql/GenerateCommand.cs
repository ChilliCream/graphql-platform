using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        var operationOutputDirArg = generate.Option(
            "-q|--operationOutputDirectory",
            "The output directory for persisted operation files.",
            CommandOptionType.SingleValue);

        var relayFormatArg = generate.Option(
            "--relayFormat",
            "Export persisted operations in the relay format.",
            CommandOptionType.NoValue);

        var jsonArg = generate.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        generate.OnExecuteAsync(
            ct =>
            {
                var strategy = RequestStrategy.Default;
                var operationOutputDir = operationOutputDirArg.Value();

                if (!string.IsNullOrEmpty(operationOutputDir) || relayFormatArg.HasValue())
                {
                    strategy = RequestStrategy.PersistedOperation;
                }

                var arguments = new GenerateCommandArguments(
                    pathArg.Value ?? CurrentDirectory,
                    rootNamespaceArg.Value(),
                    !disableSchemaValidationArg.HasValue(),
                    hashAlgorithmArg.Value() ?? "md5",
                    true,
                    disableStoreArg.HasValue(),
                    razorComponentsArg.HasValue(),
                    outputDirArg.Value(),
                    strategy,
                    operationOutputDir,
                    relayFormatArg.HasValue());
                var handler = CommandTools.CreateHandler<GenerateCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, ct);
            });
    }

    private sealed class GenerateCommandHandler : CommandHandler<GenerateCommandArguments>
    {
        private static readonly MD5 _md5 = MD5.Create();

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
            var buildArtifacts = GetBuildArtifacts(args.Path);

            foreach (var configFileName in GetConfigFiles(args.Path, buildArtifacts))
            {
                var configDir = Path.GetDirectoryName(configFileName)!;
                var configBody = await File.ReadAllTextAsync(configFileName, cancellationToken);
                var config = GraphQLConfig.FromJson(configBody);
                var clientName = config.Extensions.StrawberryShake.Name;
                var rootNamespace = args.RootNamespace ?? $"{clientName}NS";
                var documents = GetGraphQLDocuments(configDir, config.Documents, buildArtifacts, config.Schema);
                var settings = CreateSettings(config, args, rootNamespace);
                var result = GenerateClient(settings.ClientName, documents, settings);
                var outputDir = args.OutputDir ??
                    Path.Combine(
                        Path.GetDirectoryName(configFileName)!,
                        config.Extensions.StrawberryShake.OutputDirectoryName ?? "Generated");
                var operationOutputDir = args.OperationOutputDir ??
                    Path.Combine(
                        Path.GetDirectoryName(configFileName)!,
                        config.Extensions.StrawberryShake.OutputDirectoryName ?? "Generated",
                        "Operations");

                if (result.HasErrors())
                {
                    statusCode = 1;
                    activity.WriteErrors(result.Errors);
                }
                else
                {
                    await WriteCodeFilesAsync(clientName, result, outputDir, cancellationToken);

                    if (args.Strategy is RequestStrategy.PersistedOperation)
                    {
                        await WritePersistedOperationsAsync(
                            result,
                            operationOutputDir,
                            args.RelayFormat,
                            cancellationToken);
                    }
                }
            }

            return statusCode;
        }

        private CSharpGeneratorResult GenerateClient(
            string clientName,
            string[] documents,
            CSharpGeneratorSettings settings)
        {
            using var activity = Output.WriteActivity($"Generate {clientName}");
            return CSharpGenerator.GenerateAsync(documents, settings).Result;
        }

        private async Task WriteCodeFilesAsync(
            string clientName,
            CSharpGeneratorResult result,
            string outputDir,
            CancellationToken cancellationToken)
        {
            var deleteList = Directory.Exists(outputDir)
                ? [..Directory.GetFiles(outputDir, $"{clientName}.*.cs"),]
                : new HashSet<string>();

            foreach (var doc in result.Documents)
            {
                if (doc.Kind is SourceDocumentKind.CSharp or SourceDocumentKind.Razor)
                {
                    var fileName = CreateCodeFileName(outputDir, doc.Path, doc.Name, doc.Kind);
                    deleteList.Remove(fileName);

                    if (await NeedsUpdateAsync(fileName, doc.SourceText, cancellationToken))
                    {
                        EnsureWeCanWriteTheFile(fileName);

                        await File.WriteAllTextAsync(
                            fileName,
                            doc.SourceText,
                            cancellationToken);

                        Output.WriteFileCreated(fileName);
                    }
                }
            }

            foreach (var oldFile in deleteList)
            {
                File.Delete(oldFile);
            }
        }

        private static async Task WritePersistedOperationsAsync(
            CSharpGeneratorResult result,
            string outputDir,
            bool relayFormat,
            CancellationToken cancellationToken)
        {
            if (relayFormat)
            {
                var map = new SortedDictionary<string, string>();

                foreach (var doc in result.Documents)
                {
                    if (doc.Kind is SourceDocumentKind.GraphQL)
                    {
                        map[doc.Hash!] = doc.SourceText;
                    }
                }

                var fileName = Path.Combine(outputDir, "operations.json");

                EnsureWeCanWriteTheFile(fileName);

                await File.WriteAllTextAsync(
                    fileName,
                    JsonSerializer.Serialize(map),
                    cancellationToken);
            }
            else
            {
                if (Directory.Exists(outputDir))
                {
                    foreach (var oldFile in Directory.GetFiles(outputDir, "*.graphql"))
                    {
                        File.Delete(oldFile);
                    }
                }

                foreach (var doc in result.Documents)
                {
                    if (doc.Kind is SourceDocumentKind.GraphQL)
                    {
                        var fileName = Path.Combine(outputDir, $"{doc.Hash}.graphql");

                        EnsureWeCanWriteTheFile(fileName);

                        await File.WriteAllTextAsync(
                            fileName,
                            doc.SourceText,
                            cancellationToken);
                    }
                }
            }
        }

        private static string CreateCodeFileName(
            string outputDir,
            string? path,
            string name,
            SourceDocumentKind kind)
        {
            var kindName =
                kind is SourceDocumentKind.CSharp
                    ? "Client"
                    : "Components";

            return path is null
                ? Path.Combine(outputDir, $"{name}.{kindName}.cs")
                : Path.Combine(outputDir, path, $"{name}.{kindName}.cs");
        }

        public static async Task<bool> NeedsUpdateAsync(
            string fileName,
            string sourceText,
            CancellationToken cancellationToken)
        {
            if (File.Exists(fileName))
            {
                var readTask = File.ReadAllBytesAsync(fileName, cancellationToken);

                var source = Encoding.UTF8.GetBytes(sourceText);
                var sourceHash = _md5.ComputeHash(source);

                var current = await readTask;
                var currentHash = _md5.ComputeHash(current);

                return !currentHash.AsSpan().SequenceEqual(sourceHash);
            }

            return true;
        }

        private static void EnsureWeCanWriteTheFile(string fileName)
        {
            var dir = Path.GetDirectoryName(fileName)!;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
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
            string? outputDir,
            RequestStrategy strategy,
            string? operationOutputDir,
            bool relayFormat)
        {
            Path = path;
            RootNamespace = rootNamespace;
            StrictSchemaValidation = strictSchemaValidation;
            HashAlgorithm = hashAlgorithm;
            UseSingleFile = useSingleFile;
            NoStore = noStore;
            RazorComponents = razorComponents;
            OutputDir = outputDir;
            Strategy = strategy;
            RelayFormat = relayFormat;
            OperationOutputDir = operationOutputDir;

            if (operationOutputDir is null && outputDir is not null)
            {
                OperationOutputDir = System.IO.Path.Combine(outputDir, "Operations");
            }
        }

        public string Path { get; }

        public string? RootNamespace { get; }

        public bool StrictSchemaValidation { get; }

        public string HashAlgorithm { get; }

        public bool UseSingleFile { get; }

        public bool NoStore { get; }

        public bool RazorComponents { get; }

        public string? OutputDir { get; }

        public RequestStrategy Strategy { get; }

        public string? OperationOutputDir { get; }

        public bool RelayFormat { get; }
    }
}
