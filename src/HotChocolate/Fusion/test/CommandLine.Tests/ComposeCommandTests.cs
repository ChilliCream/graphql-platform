using System.Collections.Concurrent;
using System.CommandLine.Parsing;
using CookieCrumble;
using HotChocolate.Fusion;
using HotChocolate.Fusion.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.Composition;
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
        await app.InvokeAsync(new[] { "compose", "-p", packageFile, "-s", subgraphPackageFile });

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

        snapshot.MatchSnapshot();
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

        var reviewConfig = demoProject.Reviews2.ToConfiguration(ReviewsExtensionSdl);
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
        await app.InvokeAsync(
            new[] { "compose", "-p", packageFile, "-s", accountSubgraphPackageFile });

        // act
        app = App.CreateBuilder().Build();
        await app.InvokeAsync(
            new[] { "compose", "-p", packageFile, "-s", reviewSubgraphPackageFile });

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

        snapshot.MatchSnapshot();
    }
}
