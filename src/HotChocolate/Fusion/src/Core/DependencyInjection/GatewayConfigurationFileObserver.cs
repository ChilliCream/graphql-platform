using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionResources;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class GatewayConfigurationFileObserver : IObservable<GatewayConfiguration>
{
    private readonly string _filename;

    public GatewayConfigurationFileObserver(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException(
                GatewayConfigurationFileObserver_FileNameNullOrEmpty,
                nameof(filename));
        }

        _filename = filename;
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
        => new FileConfigurationSession(observer, _filename);

    private sealed class FileConfigurationSession : IDisposable
    {
        private readonly IObserver<GatewayConfiguration> _observer;
        private readonly string _fileName;
        private readonly FileSystemWatcher _watcher;

        public FileConfigurationSession(IObserver<GatewayConfiguration> observer, string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);
            var directory = Path.GetDirectoryName(fullPath);

            _observer = observer;
            _fileName = fullPath;

            BeginLoadConfig();

            if (directory is null)
            {
                throw new FileNotFoundException(
                    FileConfigurationSession_MustContainPath,
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
                        var document = await GatewayConfigurationFileUtils.LoadDocumentAsync(_fileName, default);
                        _observer.OnNext(new GatewayConfiguration(document));
                    }
                    catch(Exception ex)
                    {
                        _observer.OnError(ex);
                    }
                });

        public void Dispose()
            => _watcher.Dispose();
    }
}
