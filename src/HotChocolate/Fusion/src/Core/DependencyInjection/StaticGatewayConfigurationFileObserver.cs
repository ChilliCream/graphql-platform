using HotChocolate.Fusion;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static Microsoft.Extensions.DependencyInjection.GatewayConfigurationFileUtils;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class StaticGatewayConfigurationFileObserver : IObservable<GatewayConfiguration>
{
    private readonly string _filename;

    public StaticGatewayConfigurationFileObserver(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(filename));
        }

        _filename = filename;
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
    {
        return new FileConfigurationSession(observer, _filename);
    }

    private class FileConfigurationSession : IDisposable
    {
        public FileConfigurationSession(IObserver<GatewayConfiguration> observer, string filename)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        var document = await LoadDocumentAsync(filename, default);
                        observer.OnNext(new GatewayConfiguration(document));
                    }
                    catch(Exception ex)
                    {
                        observer.OnError(ex);
                        observer.OnCompleted();
                    }
                });
        }

        public void Dispose()
        {
        }
    }
}

internal sealed class GatewayConfigurationFileObserver : IObservable<GatewayConfiguration>
{
    private readonly string _filename;

    public GatewayConfigurationFileObserver(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(filename));
        }

        _filename = filename;
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
    {
        return new FileConfigurationSession(observer, _filename);
    }

    private sealed class FileConfigurationSession : IDisposable
    {
        private readonly IObserver<GatewayConfiguration> _observer;
        private readonly string _fileName;
        private readonly FileSystemWatcher _watcher;

        public FileConfigurationSession(IObserver<GatewayConfiguration> observer, string fileName)
        {
            _observer = observer;
            _fileName = fileName;
            var fullPath = Path.GetFullPath(fileName);
            var directory = Path.GetDirectoryName(fullPath);

            if (directory is null)
            {
                // TODO : resources
                throw new FileNotFoundException(
                    "The file name must contain a directory path.",
                    fileName);
            }

            _watcher = new FileSystemWatcher();
            _watcher.Path = directory;
            _watcher.Filter = "*.*";

            _watcher.NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size;

            _watcher.Created += (_, e) =>
            {
                if (fullPath.EqualsOrdinal(e.FullPath))
                {
                    BeginLoadConfig();
                }
            };

            _watcher.Changed += (_, e) =>
            {
                if (fullPath.EqualsOrdinal(e.FullPath))
                {
                    BeginLoadConfig();
                }
            };

            _watcher.EnableRaisingEvents = true;
        }

        private void BeginLoadConfig()
            => Task.Run(
                async () =>
                {
                    try
                    {
                        var document = await LoadDocumentAsync(_fileName, default);
                        _observer.OnNext(new GatewayConfiguration(document));
                    }
                    catch(Exception ex)
                    {
                        _observer.OnError(ex);
                        _observer.OnCompleted();
                    }
                });

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}

internal static class GatewayConfigurationFileUtils
{
    public static async ValueTask<DocumentNode> LoadDocumentAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // We first try to load the file name as a fusion graph package.
            // This might fails as a the file that was provided is a fusion
            // graph document.
            await using var package = FusionGraphPackage.Open(fileName, FileAccess.Read);
            return await package.GetFusionGraphAsync(cancellationToken);
        }
        catch
        {
            // If we fail to load the file as a fusion graph package we will
            // try to load it as a GraphQL schema document.
            var sourceText = await File.ReadAllTextAsync(fileName, cancellationToken);
            return Utf8GraphQLParser.Parse(sourceText);
        }
    }
}
