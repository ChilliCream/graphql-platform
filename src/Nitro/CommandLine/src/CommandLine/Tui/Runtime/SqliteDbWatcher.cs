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
    /// Invoked once, synchronously, right after the pre-enable file state is
    /// captured and before the underlying watcher starts raising events.
    /// </summary>
    internal Action? OnBaselineCaptured { get; init; }

    /// <summary>
    /// Watches the database file until <paramref name="cancellationToken"/> is
    /// cancelled. When the parent directory does not exist or the file system does
    /// not support watching it, this returns without writing anything, so the
    /// caller degrades silently to manual refresh. A write landing before the
    /// watcher starts raising events is still reconciled once it does.
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

        // The main file's own reconciliation baseline: a write landing in the
        // enable gap moves its mtime (and usually its length) the same way an
        // ordinary post-enable write does, per the type-level remarks. Captured
        // alongside lastWalSize, before OnBaselineCaptured fires, so a test can
        // land a write deterministically inside the gap.
        var (lastMainWriteTimeUtc, lastMainLength) = GetFileState(_databasePath);
        var mainDatabaseChanged = false;
        var walChanged = false;

        OnBaselineCaptured?.Invoke();

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

            // A write landing between the pre-enable baseline above and the
            // watcher actually raising events would otherwise fire no event
            // while the baseline already contains its growth, losing it for
            // good. Re-reading the main file's state and the -wal size now and
            // comparing them to that same pre-enable baseline closes the
            // window for both files. Whichever path notices first wins, and a
            // duplicate DataChangedEvent is harmless since consumers treat it
            // as "re-read", not as a delta.
            var reconciledMainState = GetFileState(_databasePath);
            var reconciledWalSize = GetFileSize(walPath);

            var mainFileChanged =
                reconciledMainState.LastWriteTimeUtc != lastMainWriteTimeUtc
                || reconciledMainState.Length != lastMainLength;

            if (mainFileChanged || reconciledWalSize > lastWalSize)
            {
                lastWalSize = reconciledWalSize;
                writer.TryWrite(new TuiEvent.DataChangedEvent());
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

    /// <summary>
    /// Returns the last write time (UTC) and length of the file at
    /// <paramref name="path"/>, or the default state when it does not exist or
    /// cannot be read, the same as <see cref="GetFileSize"/>.
    /// </summary>
    private static (DateTime LastWriteTimeUtc, long Length) GetFileState(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? (info.LastWriteTimeUtc, info.Length) : (DateTime.MinValue, 0);
        }
        catch (IOException)
        {
            return (DateTime.MinValue, 0);
        }
    }
}
