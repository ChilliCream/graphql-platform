using System.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Utilities;
using static System.IO.Path;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;
using static HotChocolate.Fusion.CommandLine.Defaults;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class SubgraphConfigSetNameCommand : Command
{
    public SubgraphConfigSetNameCommand() : base("name")
    {
        Description = "Set the name of a subgraph.";

        var subgraphName = new Argument<string>("name")
        {
            Description = "The subgraph name."
        };

        var configFile = new SubgraphConfigFileOption();
        var workingDirectory = new WorkingDirectoryOption();

        AddArgument(subgraphName);
        AddOption(configFile);
        AddOption(workingDirectory);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            subgraphName,
            configFile,
            workingDirectory,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        string subgraphName,
        FileInfo? configFile,
        DirectoryInfo workingDirectory,
        CancellationToken cancellationToken)
    {
        configFile ??= new FileInfo(Combine(workingDirectory.FullName, ConfigFile));

        if (!configFile.Exists)
        {
            var config = new SubgraphConfigurationDto(subgraphName);
            var configJson = FormatSubgraphConfig(config);
            await File.WriteAllTextAsync(configFile.FullName, configJson, cancellationToken);
        }
        else if (configFile.Extension.EqualsOrdinal(".fsp"))
        {
            var config = await LoadSubgraphConfigFromSubgraphPackageAsync(configFile.FullName, cancellationToken);
            
            await ReplaceSubgraphConfigInSubgraphPackageAsync(
                configFile.FullName,
                config with { Name = subgraphName, });
        }
        else
        {
            var config = await LoadSubgraphConfigAsync(configFile.FullName, cancellationToken);
            var configJson = FormatSubgraphConfig(config with { Name = subgraphName, });
            await File.WriteAllTextAsync(configFile.FullName, configJson, cancellationToken);
        }
    }
}
