using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Fusion.Composition.Settings;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public static class FusionGatewayConfigurationUtilities
{
    public static async Task ConfigureAsync(
        IReadOnlyList<GatewayInfo> gateways,
        CancellationToken cancellationToken = default)
    {
        ExportSubgraphSchemaDocs(gateways);
        await EnsureSubgraphHasConfigAsync(gateways, cancellationToken);
        await ComposeAsync(gateways, cancellationToken);
    }

    private static void ExportSubgraphSchemaDocs(IReadOnlyList<GatewayInfo> gateways)
    {
        var processed = new HashSet<string>();

        foreach (var gateway in gateways)
        {
            foreach (var subgraph in gateway.Subgraphs)
            {
                if (!processed.Add(subgraph.Path))
                {
                    continue;
                }

                Console.WriteLine("Expoorting schema document for subgraph {0} ...", subgraph.Name);

                var workingDirectory = System.IO.Path.GetDirectoryName(subgraph.Path)!;

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "dotnet run --no-build --no-restore -- schema export --output schema.graphql",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (var process = Process.Start(processStartInfo)!)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var errors = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine(output);
                    }

                    if (!string.IsNullOrEmpty(errors))
                    {
                        Console.WriteLine(errors);
                    }

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine(
                            "{0}(1,1): error HF1002: ; Failed to export schema document for subgraph {1} ...",
                            subgraph.Path,
                            subgraph.Name);
                        Environment.Exit(-255);
                    }
                }
            }
        }
    }

    private static async Task EnsureSubgraphHasConfigAsync(
        IReadOnlyList<GatewayInfo> gateways,
        CancellationToken ct)
    {
        foreach (var gateway in gateways)
        {
            foreach (var project in gateway.Subgraphs)
            {
                var projectRoot = System.IO.Path.GetDirectoryName(project.Path)!;
                var configFile = System.IO.Path.Combine(projectRoot, WellKnownFileNames.ConfigFile);

                if (File.Exists(configFile))
                {
                    continue;
                }

                var config = new SubgraphConfigurationDto(
                    project.Name,
                    [new HttpClientConfiguration(new Uri("http://localhost:5000"), "http"),]);
                var configJson = PackageHelper.FormatSubgraphConfig(config);
                await File.WriteAllTextAsync(configFile, configJson, ct);
            }
        }
    }

    private static async Task ComposeAsync(
        IReadOnlyList<GatewayInfo> gateways,
        CancellationToken ct)
    {
        foreach (var gateway in gateways)
        {
            await ComposeGatewayAsync(gateway.Path, gateway.Subgraphs.Select(t => t.Path), ct);
        }
    }

    private static async Task ComposeGatewayAsync(
        string gatewayProject,
        IEnumerable<string> subgraphProjects,
        CancellationToken ct)
    {
        var gatewayDirectory = System.IO.Path.GetDirectoryName(gatewayProject)!;
        var packageFileName = System.IO.Path.Combine(gatewayDirectory, $"gateway{WellKnownFileExtensions.FusionPackage}");
        var packageFile = new FileInfo(packageFileName);
        var settingsFileName = System.IO.Path.Combine(gatewayDirectory, "gateway-settings.json");
        var settingsFile = new FileInfo(settingsFileName);
        var subgraphDirectories = subgraphProjects.Select(t => System.IO.Path.GetDirectoryName(t)!).ToArray();

        // Ensure Gateway Project Directory Exists.
        if (!Directory.Exists(gatewayDirectory))
        {
            Directory.CreateDirectory(gatewayDirectory);
        }

        if (packageFile.Exists)
        {
            packageFile.Delete();
        }

        await using var package = FusionGraphPackage.Open(packageFile.FullName);
        var subgraphConfigs = (await package.GetSubgraphConfigurationsAsync(ct)).ToDictionary(t => t.Name);
        await ResolveSubgraphPackagesAsync(subgraphDirectories, subgraphConfigs, ct);

        using var settingsJson = settingsFile.Exists
            ? JsonDocument.Parse(await File.ReadAllTextAsync(settingsFile.FullName, ct))
            : await package.GetFusionGraphSettingsAsync(ct);
        var settings = settingsJson.Deserialize<PackageSettings>() ?? new PackageSettings();

        var features = settings.CreateFeatures();

        var composer = new FusionGraphComposer(
            settings.FusionTypePrefix,
            settings.FusionTypeSelf,
            () => new ConsoleLog());

        var fusionGraph = await composer.TryComposeAsync(subgraphConfigs.Values, features, ct);

        if (fusionGraph is null)
        {
            Console.WriteLine("Fusion graph composition failed.");
            return;
        }

        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, typeNames)!;

        using var updateSettingsJson = JsonSerializer.SerializeToDocument(
            settings,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await package.SetFusionGraphAsync(fusionGraphDoc, ct);
        await package.SetFusionGraphSettingsAsync(updateSettingsJson, ct);
        await package.SetSchemaAsync(schemaDoc, ct);

        foreach (var config in subgraphConfigs.Values)
        {
            await package.SetSubgraphConfigurationAsync(config, ct);
        }

        Console.WriteLine("Fusion graph composed.");
    }

    private static async Task ResolveSubgraphPackagesAsync(
        IReadOnlyList<string> subgraphDirectories,
        IDictionary<string, SubgraphConfiguration> subgraphConfigs,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < subgraphDirectories.Count; i++)
        {
            var path = subgraphDirectories[i];

            if (!Directory.Exists(path))
            {
                continue;
            }

            var configFile = System.IO.Path.Combine(path, WellKnownFileNames.ConfigFile);
            var schemaFile = System.IO.Path.Combine(path, WellKnownFileNames.SchemaFile);
            var extensionFile = System.IO.Path.Combine(path, WellKnownFileNames.ExtensionFile);

            if (!File.Exists(configFile) || !File.Exists(schemaFile))
            {
                continue;
            }

            var conf = await PackageHelper.LoadSubgraphConfigAsync(configFile, cancellationToken);
            var schema = await File.ReadAllTextAsync(schemaFile, cancellationToken);
            var extensions = Array.Empty<string>();

            if (File.Exists(extensionFile))
            {
                extensions = [await File.ReadAllTextAsync(extensionFile, cancellationToken),];
            }

            subgraphConfigs[conf.Name] =
                new SubgraphConfiguration(
                    conf.Name,
                    schema,
                    extensions,
                    conf.Clients,
                    conf.Extensions);
        }
    }
}
