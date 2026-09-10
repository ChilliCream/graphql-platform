using System.Threading.Channels;

namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// A <see cref="TuiEventSource"/> that watches an agent workspace's SQLite database
/// file and publishes a debounced <see cref="TuiEvent.DataChangedEvent"/> whenever
/// the on-disk data may have changed. The parent directory is watched rather than
/// the file itself so that a full file replacement (not just an in-place write) is
/// also caught. The <c>-shm</c> sibling is deliberately excluded: every store
/// connection in this codebase opens without pooling and is disposed again after a
/// single query, so even a plain read makes SQLite create, checkpoint, and delete
/// it as that connection closes, the same file churn a real write produces. The
/// main database file's own mtime does not move for that checkpoint-of-nothing,
/// only when a write actually lands, so that alone distinguishes a real data
/// change from a store read triggering this watcher on itself. The <c>-wal</c>
/// sibling is watched as well, but only a size increase counts: a plain read also
/// creates and deletes it, but never grows it past its own prior size, while a
/// write that cannot checkpoint yet (a concurrent reader holds a lock) leaves it
/// grown, which is otherwise invisible to the main file's own mtime.
/// </summary>
internal sealed class SqliteDbWatcher(string databasePath, TimeSpan? debounce = null)
{
    private static readonly TimeSpan s_defaultDebounce = TimeSpan.FromMilliseconds(200);

    private readonly string _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    private readonly TimeSpan _debounce = debounce ?? s_defaultDebounce;

    /// <summary>
    /// Watches the database file until <paramref name="cancellationToken"/> is
    /// cancelled. When the parent directory does not exist or the file system does
    /// not support watching it, this returns without writing anything, so the
    /// caller degrades silently to manual refresh.
    /// </summary>
    public async Task RunAsync(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_databasePath));

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        FileSystemWatcher watcher;

        try
        {
            watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or PlatformNotSupportedException)
        {
            return;
        }

        var databaseFileName = Path.GetFileName(_databasePath);
        var walFileName = databaseFileName + "-wal";
        var walPath = _databasePath + "-wal";

        // A write's appended frames only count as a real change once they exceed
        // whatever the -wal file already held, so a plain read's own create and
        // delete of it (which never grows past that baseline) stays silent. See
        // the type-level remarks.
        var lastWalSize = GetFileSize(walPath);
        var mainDatabaseChanged = false;
        var walChanged = false;

        void OnTick(object? _)
        {
            if (mainDatabaseChanged)
            {
                mainDatabaseChanged = false;
                walChanged = false;
                lastWalSize = GetFileSize(walPath);
                writer.TryWrite(new TuiEvent.DataChangedEvent());
                return;
            }

            if (walChanged)
            {
                walChanged = false;
                var currentWalSize = GetFileSize(walPath);
                var grew = currentWalSize > lastWalSize;
                lastWalSize = currentWalSize;

                if (grew)
                {
                    writer.TryWrite(new TuiEvent.DataChangedEvent());
                }
            }
        }

        await using var timer = new Timer(OnTick);

        void OnEvent(object sender, FileSystemEventArgs e)
        {
            if (e.Name == databaseFileName)
            {
                mainDatabaseChanged = true;
                timer.Change(_debounce, Timeout.InfiniteTimeSpan);
            }
            else if (e.Name == walFileName)
            {
                walChanged = true;
                timer.Change(_debounce, Timeout.InfiniteTimeSpan);
            }
        }

        watcher.Created += OnEvent;
        watcher.Changed += OnEvent;
        watcher.Renamed += OnEvent;

        try
        {
            try
            {
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex) when (ex is IOException or PlatformNotSupportedException or UnauthorizedAccessException)
            {
                return;
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
        finally
        {
            watcher.Created -= OnEvent;
            watcher.Changed -= OnEvent;
            watcher.Renamed -= OnEvent;
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Returns the size of the file at <paramref name="path"/>, or zero when it
    /// does not exist or cannot be read.
    /// </summary>
    private static long GetFileSize(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
