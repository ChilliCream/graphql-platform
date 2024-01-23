using System.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Fusion.Composition;
using HotChocolate.Utilities;
using static System.IO.Path;
using static HotChocolate.Fusion.CommandLine.Defaults;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class SubgraphConfigSetWebSocketCommand : Command
{
    public SubgraphConfigSetWebSocketCommand() : base("web-socket")
    {
        Description = "Set the graphql-ws settings for the subgraph.";

        var url = new Option<Uri>("--url")
        {
            Description = "The url of the graphql-ws endpoint.",
            IsRequired = true
        };

        var clientName = new Option<string>("--client-name")
        {
            Description = "The name of graphql-ws configuration.",
            IsRequired = false
        };

        var configFile = new SubgraphConfigFileOption();
        var workingDirectory = new WorkingDirectoryOption();

        AddOption(url);
        AddOption(clientName);
        AddOption(configFile);
        AddOption(workingDirectory);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            url,
            clientName,
            configFile,
            workingDirectory,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        Uri uri,
        string clientName,
        FileInfo? configFile,
        DirectoryInfo workingDirectory,
        CancellationToken cancellationToken)
    {
        configFile ??= new FileInfo(Combine(workingDirectory.FullName, ConfigFile));

        if (!configFile.Exists)
        {
            var config = new SubgraphConfigurationDto(
                SubgraphName,
                new[]
                {
                    new WebSocketClientConfiguration(uri, clientName)
                });
            var configJson = FormatSubgraphConfig(config);
            await File.WriteAllTextAsync(configFile.FullName, configJson, cancellationToken);
        }
        else if (configFile.Extension.EqualsOrdinal(".fsp"))
        {
            var config = await LoadSubgraphConfigFromSubgraphPackageAsync(configFile.FullName, cancellationToken);
            
            var clients = config.Clients.ToList();
            
            clients.RemoveAll(t => t is WebSocketClientConfiguration);
            clients.Add(new WebSocketClientConfiguration(uri, clientName));

            await ReplaceSubgraphConfigInSubgraphPackageAsync(
                configFile.FullName,
                config with { Clients = clients, });
        }
        else
        {
            var config = await LoadSubgraphConfigAsync(configFile.FullName, cancellationToken);

            var clients = config.Clients.ToList();
            
            clients.RemoveAll(t => t is WebSocketClientConfiguration);
            clients.Add(new WebSocketClientConfiguration(uri, clientName));

            var configJson = FormatSubgraphConfig(config with { Clients = clients, });
            await File.WriteAllTextAsync(configFile.FullName, configJson, cancellationToken);
        }
    }
}
