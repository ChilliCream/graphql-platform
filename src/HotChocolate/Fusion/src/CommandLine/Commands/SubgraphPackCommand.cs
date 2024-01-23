using System.CommandLine;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Fusion.CommandLine.Options;
using HotChocolate.Utilities;
using static System.IO.Path;
using static HotChocolate.Fusion.CommandLine.Defaults;
using static HotChocolate.Fusion.CommandLine.Helpers.PackageHelper;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class SubgraphPackCommand : Command
{
    public SubgraphPackCommand() : base("pack")
    {
        Description = "Creates a Fusion subgraph package.";

        var packageFile = new SubgraphPackageFileOption();
        var schemaFile = new SubgraphSchemaFileOption();
        var configFile = new SubgraphConfigFileOption();
        var extensionFiles = new SubgraphExtensionFileOption();
        var workingDirectory = new WorkingDirectoryOption();

        AddOption(packageFile);
        AddOption(schemaFile);
        AddOption(configFile);
        AddOption(extensionFiles);
        AddOption(workingDirectory);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            packageFile,
            schemaFile,
            configFile,
            extensionFiles,
            workingDirectory,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        FileInfo? packageFile,
        FileInfo? schemaFile,
        FileInfo? configFile,
        List<FileInfo>? extensionFiles,
        DirectoryInfo workingDirectory,
        CancellationToken cancellationToken)
    {
        schemaFile ??= new FileInfo(Combine(workingDirectory.FullName, SchemaFile));
        configFile ??= new FileInfo(Combine(workingDirectory.FullName, ConfigFile));
        extensionFiles ??=
        [
            new FileInfo(Combine(workingDirectory.FullName, ExtensionFile)),
        ];

        if (!schemaFile.Exists)
        {
            console.WriteLine($"The schema file `{schemaFile.FullName}` does not exist.");
            return;
        }

        if (!configFile.Exists)
        {
            console.WriteLine($"The config file `{configFile.FullName}` does not exist.");
            return;
        }

        if (extensionFiles.Count == 0)
        {
            extensionFiles.Add(new FileInfo(Combine(workingDirectory.FullName, ExtensionFile)));
        }

        if (extensionFiles.Count == 1 &&
            extensionFiles[0].Name.EqualsOrdinal("schema.extensions.graphql") &&
            !extensionFiles[0].Exists)
        {
            extensionFiles.Clear();
        }

        if (extensionFiles.Any(t => !t.Exists))
        {
            console.WriteLine(
                $"The extension file `{extensionFiles.First(t => !t.Exists).FullName}` does not exist.");
            return;
        }

        if (packageFile is null)
        {
            var config = await LoadSubgraphConfigAsync(configFile.FullName, cancellationToken);
            var fileName = $"{config.Name}{Extensions.SubgraphPackage}";
            packageFile = new FileInfo(Combine(workingDirectory.FullName, fileName));
        }
        else if (!packageFile.Extension.EqualsOrdinal(Extensions.SubgraphPackage) &&
            !packageFile.Extension.EqualsOrdinal(Extensions.ZipPackage))
        {
            packageFile = new FileInfo($"{packageFile.FullName}{Extensions.SubgraphPackage}");
        }

        if (packageFile.Exists)
        {
            packageFile.Delete();
        }

        if (packageFile.Directory is not null && !packageFile.Directory.Exists)
        {
            packageFile.Directory.Create();
        }

        await CreateSubgraphPackageAsync(
            packageFile.FullName,
            new SubgraphFiles(
                schemaFile.FullName,
                configFile.FullName,
                extensionFiles.Select(t => t.FullName).ToList()),
            cancellationToken);

        console.WriteLine($"{packageFile.FullName} created.");
    }
}
