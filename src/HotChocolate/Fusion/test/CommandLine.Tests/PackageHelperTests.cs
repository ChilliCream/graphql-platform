using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
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
        Match(accountConfig);
        Match(accountConfigRead);
    }

    [Fact]
    public async Task Create_Extract_Extensions()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var extensions = JsonDocument.Parse("{ \"foo\": \"bar\" }").RootElement;
        var accountConfig = demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, extensions);
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
        Match(accountConfig);
        Match(accountConfigRead);
    }

    private void Match(SubgraphConfiguration config)
    {
        var snapshot = new Snapshot();
        snapshot.Add(config.Name, nameof(config.Name));
        snapshot.Add(config.Schema, nameof(config.Schema));

        foreach (var extension in config.Extensions)
        {
            snapshot.Add(extension, nameof(config.Extensions));
        }

        foreach (var client in config.Clients)
        {
            snapshot.Add(client, nameof(config.Clients));
        }

        if (config.ConfigurationExtensions is not null)
        {
            using var stream = new MemoryStream();
            using var writer =
                new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, });
            config.ConfigurationExtensions.Value.WriteTo(writer);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            snapshot.Add(json, nameof(config.ConfigurationExtensions));
        }

        snapshot.Match();
    }
}
