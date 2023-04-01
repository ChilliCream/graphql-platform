using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class FileWatcherTypeModule : ITypeModule, IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public event EventHandler<EventArgs>? TypesChanged;

    public FileWatcherTypeModule(string fileName)
    {
        if (fileName is null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _watcher = new FileSystemWatcher(fileName);

        _watcher.NotifyFilter =
            NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.FileName |
            NotifyFilters.LastWrite |
            NotifyFilters.Size;

        _watcher.Created += (s, e) => TypesChanged?.Invoke(s, e);
        _watcher.Changed += (s, e) => TypesChanged?.Invoke(s, e);
        _watcher.EnableRaisingEvents = true;
    }

    public ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        return new ValueTask<IReadOnlyCollection<ITypeSystemMember>>(
            Array.Empty<ITypeSystemMember>());
    }

    public void Dispose()
        => _watcher.Dispose();
}
