using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class FileWatcherTypeModule : TypeModule, IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public FileWatcherTypeModule(string fileName)
    {
        if (fileName is null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

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
                OnTypesChanged();
            }
        };

        _watcher.Changed += (_, e) =>
        {
            if (fullPath.EqualsOrdinal(e.FullPath))
            {
                OnTypesChanged();
            }
        };

        _watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
        => _watcher.Dispose();
}
