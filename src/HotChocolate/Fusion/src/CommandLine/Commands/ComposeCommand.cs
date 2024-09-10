using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Utilities;
using IOPath = System.IO.Path;
using static System.Text.Json.JsonSerializerDefaults;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class ComposeCommand : Command
{
    [RequiresUnreferencedCode(
        "Calls HotChocolate.Fusion.CommandLine.Commands.ComposeCommand.ExecuteAsync(IConsole, FileInfo, " +
        "List<String>, List<String>, FileInfo, DirectoryInfo, Boolean?, CancellationToken)")]
    public ComposeCommand() : base("compose")
    {
        var fusionPackageFile = new Option<FileInfo>("--package-file") { IsRequired = true, };
        fusionPackageFile.AddAlias("--package");
        fusionPackageFile.AddAlias("-p");

        var subgraphPackageFile = new Option<List<string>?>("--subgraph-package-file");
        subgraphPackageFile.AddAlias("--subgraph");
        subgraphPackageFile.AddAlias("-s");

        var fusionPackageSettingsFile = new Option<FileInfo?>("--package-settings-file");
        fusionPackageSettingsFile.AddAlias("--package-settings");
        fusionPackageSettingsFile.AddAlias("--settings");

        var removeSubgraphs = new Option<List<string>?>("--remove");
        removeSubgraphs.AddAlias("-r");

        var workingDirectory = new WorkingDirectoryOption();

        var enableNodes = new Option<bool?>("--enable-nodes");
        enableNodes.Arity = ArgumentArity.Zero;

        AddOption(fusionPackageFile);
        AddOption(subgraphPackageFile);
        AddOption(fusionPackageSettingsFile);
        AddOption(workingDirectory);
        AddOption(enableNodes);
        AddOption(removeSubgraphs);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            fusionPackageFile,
            subgraphPackageFile,
            removeSubgraphs,
            fusionPackageSettingsFile,
            workingDirectory,
            enableNodes,
            Bind.FromServiceProvider<CancellationToken>());
    }

    [RequiresUnreferencedCode(
        "Calls System.Text.Json.JsonSerializer.SerializeToDocument<TValue>(TValue, JsonSerializerOptions)")]
    private static async Task<int> ExecuteAsync(
        IConsole console,
        FileInfo packageFile,
        List<string>? subgraphPackageFiles,
        List<string>? removeSubgraphs,
        FileInfo? settingsFile,
        DirectoryInfo workingDirectory,
        bool? enableNodes,
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
            var settingsFileName = IOPath.GetFileNameWithoutExtension(packageFile.FullName) + "-settings.json";

            if (packageFile.DirectoryName is not null)
            {
                settingsFileName = IOPath.Combine(packageFile.DirectoryName, settingsFileName);
            }

            settingsFile = new FileInfo(settingsFileName);
        }

        await using var package = FusionGraphPackage.Open(packageFile.FullName);

        if (removeSubgraphs is not null)
        {
            foreach (var subgraph in removeSubgraphs)
            {
                await package.RemoveSubgraphConfigurationAsync(subgraph, cancellationToken);
            }
        }

        var configs = (await package.GetSubgraphConfigurationsAsync(cancellationToken)).ToDictionary(t => t.Name);

        // resolve subgraph packages will scan the directory for FSPs. In case of remove we don't want to do that.
        if (removeSubgraphs is not { Count: > 0, } || subgraphPackageFiles is { Count: > 0, })
        {
            await ResolveSubgraphPackagesAsync(workingDirectory, subgraphPackageFiles, configs, cancellationToken);
        }

        using var settingsJson = settingsFile.Exists
            ? JsonDocument.Parse(await File.ReadAllTextAsync(settingsFile.FullName, cancellationToken))
            : await package.GetFusionGraphSettingsAsync(cancellationToken);
        var settings = settingsJson.Deserialize<PackageSettings>();

        if (settings is null)
        {
            console.WriteLine("Fusion graph settings are invalid.");
            return 1;
        }

        if (enableNodes.HasValue && enableNodes.Value)
        {
            settings.NodeField.Enabled = true;
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
            return 1;
        }

        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new Metadata.FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;
        using var updateSettingsJson = JsonSerializer.SerializeToDocument(settings, new JsonSerializerOptions(Web));

        await package.SetFusionGraphAsync(fusionGraphDoc, cancellationToken);
        await package.SetFusionGraphSettingsAsync(updateSettingsJson, cancellationToken);
        await package.SetSchemaAsync(schemaDoc, cancellationToken);

        foreach (var config in configs.Values)
        {
            await package.SetSubgraphConfigurationAsync(config, cancellationToken);
        }

        console.WriteLine("Fusion graph composed.");

        return 0;
    }

    private static FusionFeatureCollection CreateFeatures(
        PackageSettings settings)
    {
        var features = new List<IFusionFeature>();

        features.Add(
            new TransportFeature
            {
                DefaultClientName = settings.Transport.DefaultClientName,
            });

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

    private static async Task ResolveSubgraphPackagesAsync(
        DirectoryInfo workingDirectory,
        IReadOnlyList<string>? subgraphPackageFiles,
        IDictionary<string, SubgraphConfiguration> configs,
        CancellationToken cancellationToken)
    {
        var temp = new List<SubgraphConfiguration>();

        // if no subgraph packages were specified we will try to find some by their extension in the
        // working directory.
        if (subgraphPackageFiles is null || subgraphPackageFiles.Count == 0)
        {
            subgraphPackageFiles = workingDirectory
                .GetFiles($"*{Extensions.SubgraphPackage}")
                .Select(t => t.FullName)
                .ToList();
        }

        if (subgraphPackageFiles.Count > 0)
        {
            for (var i = 0; i < subgraphPackageFiles.Count; i++)
            {
                var file = subgraphPackageFiles[i];

                // if the specified subgraph package path is a directory
                // we will try to resolve the subgraph package by its extension
                // from the specified directory.
                if (!File.Exists(file) && Directory.Exists(file))
                {
                    var files = Directory
                        .EnumerateFiles(file, $"*{Extensions.SubgraphPackage}")
                        .ToList();

                    if (files.Count == 0)
                    {
                        var configFile = IOPath.Combine(file, Defaults.ConfigFile);
                        var schemaFile = IOPath.Combine(file, Defaults.SchemaFile);
                        var extensionFile = IOPath.Combine(file, Defaults.ExtensionFile);

                        if (File.Exists(configFile) && File.Exists(schemaFile))
                        {
                            var conf = await LoadSubgraphConfigAsync(configFile, cancellationToken);
                            var schema = await File.ReadAllTextAsync(schemaFile, cancellationToken);
                            var extensions = Array.Empty<string>();

                            if (File.Exists(extensionFile))
                            {
                                extensions = [await File.ReadAllTextAsync(extensionFile, cancellationToken),];
                            }

                            temp.Add(
                                new SubgraphConfiguration(
                                    conf.Name,
                                    schema,
                                    extensions,
                                    conf.Clients,
                                    conf.Extensions));
                        }
                    }
                    else
                    {
                        foreach (var packageFile in files)
                        {
                            var conf = await ReadSubgraphPackageAsync(packageFile, cancellationToken);
                            temp.Add(conf);
                        }
                    }
                }
                else if (File.Exists(file))
                {
                    var conf = await ReadSubgraphPackageAsync(file, cancellationToken);
                    temp.Add(conf);
                }
            }
        }

        foreach (var config in temp)
        {
            configs[config.Name] = config;
        }
    }

    private sealed class ConsoleLog(IConsole console) : ICompositionLog
    {
        public bool HasErrors { get; private set; }

        public void Write(LogEntry e)
        {
            if (e.Severity is LogSeverity.Error)
            {
                HasErrors = true;
            }

            var writer = console.Out;
            if (e.Severity == LogSeverity.Error)
            {
                writer = console.Error;
            }

            if (e.Code is null)
            {
                writer.WriteLine($"{e.Severity}: {e.Message}");
            }
            else if (e.Coordinate is null)
            {
                writer.WriteLine($"{e.Severity}: {e.Code} {e.Message}");
            }
            else
            {
                writer.WriteLine($"{e.Severity}: {e.Code} {e.Message} {e.Coordinate}");
            }
        }
    }

    private class PackageSettings
    {
        private Feature? _reEncodeIds;
        private Feature? _nodeField;
        private TagDirective? _tagDirective;
        private Transport? _transport;

        [JsonPropertyName("fusionTypePrefix")]
        [JsonPropertyOrder(10)]
        public string? FusionTypePrefix { get; set; }

        [JsonPropertyName("fusionTypeSelf")]
        [JsonPropertyOrder(11)]
        public bool FusionTypeSelf { get; set; }

        public Transport Transport
        {
            get => _transport ??= new();
            set => _transport = value;
        }

        [JsonPropertyName("nodeField")]
        [JsonPropertyOrder(12)]
        public Feature NodeField
        {
            get => _nodeField ??= new();
            set => _nodeField = value;
        }

        [JsonPropertyName("reEncodeIds")]
        [JsonPropertyOrder(13)]
        public Feature ReEncodeIds
        {
            get => _reEncodeIds ??= new();
            set => _reEncodeIds = value;
        }

        [JsonPropertyName("tagDirective")]
        [JsonPropertyOrder(14)]
        public TagDirective TagDirective
        {
            get => _tagDirective ??= new();
            set => _tagDirective = value;
        }
    }

    private class Feature
    {
        [JsonPropertyName("enabled")]
        [JsonPropertyOrder(10)]
        public bool Enabled { get; set; }
    }

    private sealed class TagDirective : Feature
    {
        private string[]? _exclude;

        [JsonPropertyName("makePublic")]
        [JsonPropertyOrder(100)]
        public bool MakePublic { get; set; }

        [JsonPropertyName("exclude")]
        [JsonPropertyOrder(101)]
        public string[] Exclude
        {
            get => _exclude ?? [];
            set => _exclude = value;
        }
    }

    public sealed class Transport
    {
        [JsonPropertyName("defaultClientName")]
        [JsonPropertyOrder(10)]
        public string? DefaultClientName { get; set; } = "Fusion";
    }
}
