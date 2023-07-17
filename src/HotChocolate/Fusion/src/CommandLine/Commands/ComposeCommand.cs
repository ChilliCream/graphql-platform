using System.CommandLine;
using System.Text.Json;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
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

        var fusionPackageSettingsFile = new Option<FileInfo?>("--package-settings-file");
        fusionPackageSettingsFile.AddAlias("--package-settings");
        fusionPackageSettingsFile.AddAlias("--settings");

        var workingDirectory = new WorkingDirectoryOption();

        AddOption(fusionPackageFile);
        AddOption(subgraphPackageFile);
        AddOption(workingDirectory);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            fusionPackageFile,
            subgraphPackageFile,
            fusionPackageSettingsFile,
            workingDirectory,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        FileInfo packageFile,
        List<FileInfo>? subgraphPackageFiles,
        FileInfo? settingsFile,
        DirectoryInfo workingDirectory,
        CancellationToken cancellationToken)
    {
        // create directory for package file.
        if (packageFile.Directory is not null && !packageFile.Directory.Exists)
        {
            packageFile.Directory.Create();
        }

        // Append file extension if not exists. 
        if (!packageFile.Extension.EqualsOrdinal(Extensions.FusionPackage) &&
            !packageFile.Extension.EqualsOrdinal(Extensions.ZipPackage))
        {
            packageFile = new FileInfo(packageFile.FullName + Extensions.FusionPackage);
        }

        if (settingsFile is null)
        {
            var settingsFileName = packageFile.Name + "-settings.json";

            if (packageFile.DirectoryName is not null)
            {
                settingsFileName = System.IO.Path.Combine(packageFile.DirectoryName, settingsFileName);
            }

            settingsFile = new FileInfo(settingsFileName);
        }

        // if no subgraph packages were specified we will try to find some by their extension in the
        // working directory.
        if (subgraphPackageFiles is null || subgraphPackageFiles.Count == 0)
        {
            subgraphPackageFiles = workingDirectory.GetFiles($"*{Extensions.SubgraphPackage}").ToList();
        }

        if (subgraphPackageFiles.Count > 0)
        {
            List<FileInfo>? remove = null;

            for (var i = 0; i < subgraphPackageFiles.Count; i++)
            {
                var file = subgraphPackageFiles[i];

                // if the specified subgraph package path is a directory
                // we will try to resolve the subgraph package by its extension
                // from the specified directory.
                if (!file.Exists && Directory.Exists(file.FullName))
                {
                    var firstFile = Directory
                        .EnumerateFiles(file.FullName, $"*{Extensions.SubgraphPackage}")
                        .FirstOrDefault();

                    if (firstFile is null)
                    {
                        (remove ??= new()).Add(file);
                    }
                    else
                    {
                        subgraphPackageFiles[i] = new FileInfo(firstFile);
                    }
                }
            }

            if (remove is { Count: > 0 })
            {
                foreach (var fileInfo in remove)
                {
                    subgraphPackageFiles.Remove(fileInfo);
                }
            }
        }

        if (subgraphPackageFiles.Any(t => !t.Exists))
        {
            console.WriteLine("Some subgraph packages do not exist.");

            foreach (var missingFile in subgraphPackageFiles.Where(t => !t.Exists))
            {
                console.WriteLine($"- {missingFile.FullName}");
            }

            return;
        }

        await using var package = FusionGraphPackage.Open(packageFile.FullName);

        var configs = (await package.GetSubgraphConfigurationsAsync(cancellationToken))
            .ToDictionary(t => t.Name);

        foreach (var subgraphPackageFile in subgraphPackageFiles)
        {
            var config = await ReadSubgraphPackageAsync(
                subgraphPackageFile.FullName,
                cancellationToken);
            configs[config.Name] = config;
        }

        using var settingsJson = settingsFile.Exists
            ? JsonDocument.Parse(await File.ReadAllTextAsync(settingsFile.FullName, cancellationToken))
            : await package.GetFusionGraphSettingsAsync(cancellationToken);
        var settings = settingsJson.Deserialize<PackageSettings>();

        if (settings is null)
        {
            console.WriteLine("Fusion graph settings are invalid.");
            return;
        }
        
        var features = CreateFeatures(settings);

        var composer = new FusionGraphComposer(
            settings.FusionTypePrefix,
            settings.FusionTypeSelf,
            () => new ConsoleLog(console));

        var fusionGraph = await composer.TryComposeAsync(configs.Values, features, cancellationToken);

        if (fusionGraph is null)
        {
            console.WriteLine("Fusion graph composition failed.");
            return;
        }

        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new Metadata.FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode) rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;

        await package.SetFusionGraphAsync(fusionGraphDoc, cancellationToken);
        await package.SetSchemaAsync(schemaDoc, cancellationToken);

        foreach (var config in configs.Values)
        {
            await package.SetSubgraphConfigurationAsync(config, cancellationToken);
        }

        console.WriteLine("Fusion graph composed.");
    }

    private static FusionFeatureCollection CreateFeatures(
        PackageSettings settings)
    {
        var features = new List<IFusionFeature>();

        if (settings.NodeField.Enabled)
        {
            features.Add(FusionFeatures.NodeField);
        }

        if (settings.ReEncodeIds.Enabled)
        {
            features.Add(FusionFeatures.ReEncodeIds);
        }

        if (settings.TagDirective.Enabled)
        {
            features.Add(
                FusionFeatures.TagDirective(
                    settings.TagDirective.Exclude, 
                    settings.TagDirective.MakePublic));
        }

        return new FusionFeatureCollection(features);
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
            else if (e.Coordinate is null)
            {
                _console.WriteLine($"{e.Severity}: {e.Code} {e.Message}");
            }
            else
            {
                _console.WriteLine($"{e.Severity}: {e.Code} {e.Message} {e.Coordinate}");
            }
        }
    }

    private class PackageSettings
    {
        public string? FusionTypePrefix { get; set; }

        public bool FusionTypeSelf { get; set; }

        public Feature NodeField { get; set; } = new();

        public Feature ReEncodeIds { get; set; } = new();

        public TagDirective TagDirective { get; set; } = new();
    }

    private class Feature
    {
        public bool Enabled { get; set; } = false;
    }

    private sealed class TagDirective : Feature
    {
        public bool MakePublic { get; set; } = false;

        public string[] Exclude { get; set; } = Array.Empty<string>();
    }
}