using System.Collections.Concurrent;
using CookieCrumble;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;

namespace CommandLine.Tests;

public class PackageHelperTests : IDisposable
{
    private readonly ConcurrentBag<string> _files = new();

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

    private Files CreateFiles(SubgraphConfiguration configuration)
    {
        var files = new Files(CreateTempFile(), CreateTempFile(), new[] { CreateTempFile() });
        var configJson = FormatSubgraphConfig(new(configuration.Name, configuration.Clients));
        File.WriteAllText(files.SchemaFile, configuration.Schema);
        File.WriteAllText(files.TransportConfigFile, configJson);
        File.WriteAllText(files.ExtensionFiles[0], configuration.Extensions[0]);
        return files;
    }

    private string CreateTempFile()
    {
        var file = Path.GetTempFileName();
        _files.Add(file);
        return file;
    }

    public void Dispose()
    {
        while (_files.TryTake(out var file))
        {
            File.Delete(file);
        }
    }

    public record Files(string SchemaFile, string TransportConfigFile, string[] ExtensionFiles);
}
