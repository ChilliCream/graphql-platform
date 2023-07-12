using System.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Utilities;
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
        AddOption(enableNodes);
        AddOption(fusionPrefix);
        AddOption(fusionPrefixSelf);
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
        if(packageFile.Directory is not null && !packageFile.Directory.Exists)
        {
            packageFile.Directory.Create();
        }

        if (!packageFile.Extension.EqualsOrdinal(Extensions.FusionPackage) &&
            !packageFile.Extension.EqualsOrdinal(Extensions.ZipPackage))
        {
            packageFile = new FileInfo(packageFile.FullName + Extensions.FusionPackage);
        }

        await using var package = FusionGraphPackage.Open(packageFile.FullName);

        if (subgraphPackageFiles is null || subgraphPackageFiles.Count == 0)
        {
            subgraphPackageFiles =
                workingDirectory.GetFiles($"*{Extensions.SubgraphPackage}").ToList();
        }

        for (var i = 0; i < subgraphPackageFiles.Count; i++)
        {
            var file = subgraphPackageFiles[i];

            if (!file.Exists && Directory.Exists(file.FullName))
            {
                var firstFile = Directory
                    .EnumerateFiles(file.FullName, $"*{Extensions.SubgraphPackage}")
                    .FirstOrDefault();

                if (firstFile is not null)
                {
                    subgraphPackageFiles[i] = new FileInfo(firstFile);
                }
            }
        }

        if (subgraphPackageFiles.Count == 0)
        {
            console.WriteLine("No subgraph packages found.");
            return;
        }

        if(subgraphPackageFiles.Any(t => !t.Exists))
        {
            console.WriteLine("Some subgraph packages do not exist.");

            foreach (var missingFile in subgraphPackageFiles.Where(t => !t.Exists))
            {
                console.WriteLine($"- {missingFile.FullName}");
            }

            return;
        }

        var configs = (await package.GetSubgraphConfigurationsAsync(cancellationToken))
            .ToDictionary(t => t.Name);

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
        var fusionGraph = await composer.TryComposeAsync(configs.Values, flags, cancellationToken);

        if (fusionGraph is null)
        {
            console.WriteLine("Fusion graph composition failed.");
            return;
        }

        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new Metadata.FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;

        await package.SetFusionGraphAsync(fusionGraphDoc, cancellationToken);
        await package.SetSchemaAsync(schemaDoc, cancellationToken);

        foreach (var config in configs.Values)
        {
            await package.SetSubgraphConfigurationAsync(config, cancellationToken);
        }

        console.WriteLine("Fusion graph composed.");
    }

    private sealed class ConsoleLog : ICompositionLog
    {
        private readonly IConsole _console;

        public ConsoleLog(IConsole console)
        {
            _console = console;
        }

        public bool HasErrors { get; private set; }

        public void Write(LogEntry e)
        {
            if (e.Severity is LogSeverity.Error)
            {
                HasErrors = true;
            }

            if (e.Code is null)
            {
                _console.WriteLine($"{e.Severity}: {e.Message}");
            }
            else if(e.Coordinate is null)
            {
                _console.WriteLine($"{e.Severity}: {e.Code} {e.Message}");
            }
            else
            {
                _console.WriteLine($"{e.Severity}: {e.Code} {e.Message} {e.Coordinate}");
            }
        }
    }
}
