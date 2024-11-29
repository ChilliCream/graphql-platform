using System.CommandLine.Parsing;
using System.Text;
using HotChocolate.Fusion;
using HotChocolate.Fusion.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.Shared;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;

namespace CommandLine.Tests;

public class ComposeCommandTests : CommandTestBase
{
    [Fact]
    public async Task Compose_Fusion_Graph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var subgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            subgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphPackageFile,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        var fusionGraph = await package.GetFusionGraphAsync();
        var schema = await package.GetSchemaAsync();
        var subgraphs = await package.GetSubgraphConfigurationsAsync();

        var snapshot = new Snapshot();

        snapshot.Add(schema, "Schema Document");
        snapshot.Add(fusionGraph, "Fusion Graph Document");

        foreach (var subgraph in subgraphs)
        {
            snapshot.Add(subgraph, $"{subgraph.Name} Subgraph Configuration");
        }

        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Compose_Fusion_Graph_Append_Subgraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var accountSubgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            accountSubgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var reviewConfig = demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl);
        var review = CreateFiles(reviewConfig);
        var reviewSubgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            reviewSubgraphPackageFile,
            new SubgraphFiles(
                review.SchemaFile,
                review.TransportConfigFile,
                review.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", accountSubgraphPackageFile,]);

        // act
        app = App.CreateBuilder().Build();
        await app.InvokeAsync(
            ["compose", "-p", packageFile, "-s", reviewSubgraphPackageFile,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        var fusionGraph = await package.GetFusionGraphAsync();
        var schema = await package.GetSchemaAsync();
        var subgraphs = await package.GetSubgraphConfigurationsAsync();

        var snapshot = new Snapshot();

        snapshot.Add(schema, "Schema Document");
        snapshot.Add(fusionGraph, "Fusion Graph Document");

        foreach (var subgraph in subgraphs)
        {
            snapshot.Add(subgraph, $"{subgraph.Name} Subgraph Configuration");
        }

        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Ensure_Default_Settings_Are_Included()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var subgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            subgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphPackageFile,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        using var settings = await package.GetFusionGraphSettingsAsync();
        settings.RootElement.ToString().MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task Ensure_Legacy_Node_Switch_Is_Recognized()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var subgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            subgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphPackageFile, "--enable-nodes",]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        using var settings = await package.GetFusionGraphSettingsAsync();
        settings.RootElement.ToString().MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task Ensure_Settings_Are_Included()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var subgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            subgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);
        var settingsFile = Path.Combine(
            Path.GetDirectoryName(packageFile)!,
            $"{Path.GetFileNameWithoutExtension(packageFile)}-settings.json");

        await File.WriteAllTextAsync(
            settingsFile,
            """
            {
              "fusionTypePrefix": null,
              "fusionTypeSelf": false,
              "nodeField": { "enabled": true },
              "tagDirective": {
                "enabled": true,
                "makePublic": true,
                "exclude": ["internal"]
              }
            }
            """);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphPackageFile,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        using var settings = await package.GetFusionGraphSettingsAsync();
        settings.RootElement.ToString().MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task Compose_Loose_Subgraph_Files()
    {
        // arrange
        var subgraphDir = CreateTempDir();
        Directory.CreateDirectory(subgraphDir);

        var schemaFile = Path.Combine(subgraphDir, "schema.graphql");
        var configFile = Path.Combine(subgraphDir, "subgraph-config.json");

        await File.WriteAllTextAsync(schemaFile, FileResource.Open("test1.graphql"), Encoding.UTF8);
        await File.WriteAllTextAsync(configFile, FileResource.Open("test1.subgraph-config.json"), Encoding.UTF8);

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphDir,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        var fusionGraph = await package.GetFusionGraphAsync();
        var schema = await package.GetSchemaAsync();
        var subgraphs = await package.GetSubgraphConfigurationsAsync();

        var snapshot = new Snapshot();

        snapshot.Add(schema, "Schema Document");
        snapshot.Add(fusionGraph, "Fusion Graph Document");

        foreach (var subgraph in subgraphs)
        {
            snapshot.Add(subgraph, $"{subgraph.Name} Subgraph Configuration");
        }

        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Compose_Fusion_Graph_Remove_Subgraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var accountSubgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            accountSubgraphPackageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        var reviewConfig = demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl);
        var review = CreateFiles(reviewConfig);
        var reviewSubgraphPackageFile = CreateTempFile();

        await PackageHelper.CreateSubgraphPackageAsync(
            reviewSubgraphPackageFile,
            new SubgraphFiles(
                review.SchemaFile,
                review.TransportConfigFile,
                review.ExtensionFiles));

        var packageFile = CreateTempFile(Extensions.FusionPackage);

        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", accountSubgraphPackageFile,]);

        app = App.CreateBuilder().Build();
        await app.InvokeAsync(
            ["compose", "-p", packageFile, "-s", reviewSubgraphPackageFile,]);

        // act
        app = App.CreateBuilder().Build();
        await app.InvokeAsync(
            ["compose", "-p", packageFile, "-r", "Reviews2",]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        var fusionGraph = await package.GetFusionGraphAsync();
        var schema = await package.GetSchemaAsync();
        var subgraphs = await package.GetSubgraphConfigurationsAsync();

        var snapshot = new Snapshot();

        snapshot.Add(schema, "Schema Document");
        snapshot.Add(fusionGraph, "Fusion Graph Document");

        foreach (var subgraph in subgraphs)
        {
            snapshot.Add(subgraph, $"{subgraph.Name} Subgraph Configuration");
        }

        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Compose_With_Tag()
    {
        // arrange
        var subgraphDir = CreateTempDir();
        Directory.CreateDirectory(subgraphDir);

        var schemaFile = Path.Combine(subgraphDir, "schema.graphql");
        var configFile = Path.Combine(subgraphDir, "subgraph-config.json");

        await File.WriteAllTextAsync(schemaFile, FileResource.Open("test2.graphql"), Encoding.UTF8);
        await File.WriteAllTextAsync(configFile, FileResource.Open("test2.subgraph-config.json"), Encoding.UTF8);

        var packageFile = CreateTempFile(Extensions.FusionPackage);
        var gatewayConfig = Path.Combine(
            Path.GetDirectoryName(packageFile)!,
            Path.GetFileNameWithoutExtension(packageFile) + "-settings.json");
        File.Delete(packageFile);

        await File.WriteAllTextAsync(gatewayConfig, FileResource.Open("test2.gateway-config.json"), Encoding.UTF8);

        // act
        var app = App.CreateBuilder().Build();
        await app.InvokeAsync(["compose", "-p", packageFile, "-s", subgraphDir,]);

        // assert
        Assert.True(File.Exists(packageFile));

        await using var package = FusionGraphPackage.Open(packageFile, FileAccess.Read);

        var fusionGraph = await package.GetFusionGraphAsync();
        var schema = await package.GetSchemaAsync();
        var subgraphs = await package.GetSubgraphConfigurationsAsync();

        var snapshot = new Snapshot();

        snapshot.Add(schema, "Schema Document");
        snapshot.Add(fusionGraph, "Fusion Graph Document");

        foreach (var subgraph in subgraphs)
        {
            snapshot.Add(subgraph, $"{subgraph.Name} Subgraph Configuration");
        }

        await snapshot.MatchMarkdownAsync();
    }
}
