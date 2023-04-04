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

public abstract class CommandTestBase : IDisposable
{
    private readonly ConcurrentBag<string> _files = new();

    protected Files CreateFiles(SubgraphConfiguration configuration)
    {
        var files = new Files(CreateTempFile(), CreateTempFile(), new[] { CreateTempFile() });
        var configJson = PackageHelper.FormatSubgraphConfig(
            new(configuration.Name, configuration.Clients));
        File.WriteAllText(files.SchemaFile, configuration.Schema);
        File.WriteAllText(files.TransportConfigFile, configJson);
        File.WriteAllText(files.ExtensionFiles[0], configuration.Extensions[0]);
        return files;
    }

    protected string CreateTempFile(string? extension = null)
    {
        var file = Path.GetTempFileName();
        _files.Add(file);

        if (extension is not null)
        {
            file += extension;
            _files.Add(file);
        }

        return file;
    }

    public void Dispose()
    {
        while (_files.TryTake(out var file))
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // we ignore errors here
                }
            }
        }
    }

    public record Files(string SchemaFile, string TransportConfigFile, string[] ExtensionFiles);
}
