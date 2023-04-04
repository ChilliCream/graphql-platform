using System.Collections.Concurrent;
using CookieCrumble;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;

namespace CommandLine.Tests;

public class PackageHelperTests : CommandTestBase
{
    [Fact]
    public async Task Create_Subgraph_Package()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl);
        var account = CreateFiles(accountConfig);
        var packageFile = CreateTempFile();

        // act
        await CreateSubgraphPackageAsync(
            packageFile,
            new SubgraphFiles(
                account.SchemaFile,
                account.TransportConfigFile,
                account.ExtensionFiles));

        // assert
        Assert.True(File.Exists(packageFile));
        var accountConfigRead = await ReadSubgraphPackageAsync(packageFile);
        accountConfig.MatchSnapshot();
        accountConfigRead.MatchSnapshot();
    }
}
