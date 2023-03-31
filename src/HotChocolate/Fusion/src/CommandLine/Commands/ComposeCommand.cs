using System.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class ComposeCommand : Command
{
    public ComposeCommand() : base("compose")
    {
        var fusionPackageFile = new Option<FileInfo>("--package-file") { IsRequired = true };
        fusionPackageFile.AddAlias("--package");
        fusionPackageFile.AddAlias("-p");

        var subgraphPackageFile = new Option<List<FileInfo>?>("--subgraph-package-file");
        subgraphPackageFile.AddAlias("--subgraph");
        subgraphPackageFile.AddAlias("-s");

        var enableNodes = new Option<bool>("--enable-nodes");
        enableNodes.Arity = ArgumentArity.Zero;

        var fusionPrefix = new Option<string?>("--fusion-prefix");
        fusionPrefix.AddAlias("--prefix");

        var fusionPrefixSelf = new Option<bool>("--fusion-prefix-self");
        fusionPrefixSelf.AddAlias("--prefix-self");
        fusionPrefixSelf.Arity = ArgumentArity.Zero;

        var workingDirectory = new WorkingDirectoryOption();

        AddOption(fusionPackageFile);
        AddOption(subgraphPackageFile);
        AddOption(workingDirectory);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            fusionPackageFile,
            subgraphPackageFile,
            enableNodes,
            fusionPrefix,
            fusionPrefixSelf,
            workingDirectory,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        FileInfo packageFile,
        List<FileInfo>? subgraphPackageFiles,
        bool enableNodes,
        string? prefix,
        bool prefixSelf,
        DirectoryInfo workingDirectory,
        CancellationToken cancellationToken)
    {
        var configs = new Dictionary<string, SubgraphConfiguration>();

        if (packageFile.Exists)
        {
            foreach (var config in await ReadSubgraphConfigsFromFusionPackageAsync(
                packageFile.FullName,
                cancellationToken))
            {
                configs[config.Name] = config;
            }
        }
        else if(packageFile.Directory is not null && !packageFile.Directory.Exists)
        {
            packageFile.Directory.Create();
        }

        if (subgraphPackageFiles is null || subgraphPackageFiles.Count == 0)
        {
            subgraphPackageFiles =
                workingDirectory.GetFiles($"*{Extensions.SubgraphPackage}").ToList();
        }

        if (subgraphPackageFiles.Count == 0)
        {
            console.WriteLine("No subgraph packages found.");
            return;
        }

        foreach (var subgraphPackageFile in subgraphPackageFiles)
        {
            var config = await ReadSubgraphPackageAsync(
                subgraphPackageFile.FullName,
                cancellationToken);
            configs[config.Name] = config;
        }

        var flags = FusionFeatureFlags.None;

        if (enableNodes)
        {
            flags |= FusionFeatureFlags.NodeField;
        }

        var composer = new FusionGraphComposer(prefix, prefixSelf, () => new ConsoleLog(console));
        var fusionGraph = await composer.ComposeAsync(configs.Values, flags, cancellationToken);
        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));

        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new Metadata.FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;

        await CreateFusionPackageAsync(
            packageFile.FullName,
            schemaDoc,
            fusionGraphDoc,
            configs.Values);
    }

    private sealed class ConsoleLog : ICompositionLog
    {
        private readonly IConsole _console;

        public ConsoleLog(IConsole console)
        {
            _console = console;
        }

        public bool HasErrors { get; private set; }

        public void Write(LogEntry entry)
        {
            if (entry.Severity is LogSeverity.Error)
            {
                HasErrors = true;
            }

            _console.WriteLine(entry.Message);
        }
    }
}
