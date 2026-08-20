using System.Threading.Channels;

namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// A <see cref="TuiEventSource"/> that watches an agent workspace's SQLite database
/// file and publishes a debounced <see cref="TuiEvent.DataChangedEvent"/> whenever
/// the on-disk data may have changed. The parent directory is watched rather than
/// the file itself so that a full file replacement (not just an in-place write) is
/// also caught. The <c>-wal</c> and <c>-shm</c> siblings are deliberately excluded:
/// every store connection in this codebase opens without pooling and is disposed
/// again after a single query, so even a plain read makes SQLite create, checkpoint,
/// and delete both siblings as that connection closes, the same file churn a real
/// write produces. The main database file's own mtime does not move for that
/// checkpoint-of-nothing, only when a write actually lands, so it alone is what
/// distinguishes a real data change from a store read triggering this watcher on
/// itself.
/// </summary>
internal sealed class SqliteDbWatcher(string databasePath, TimeSpan? debounce = null)
{
    private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(200);

    private readonly string _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    private readonly TimeSpan _debounce = debounce ?? DefaultDebounce;

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
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or PlatformNotSupportedException)
        {
            return;
        }

        await using var timer = new Timer(_ => writer.TryWrite(new TuiEvent.DataChangedEvent()));
        var databaseFileName = Path.GetFileName(_databasePath);

        void OnEvent(object sender, FileSystemEventArgs e)
        {
            if (IsRelevant(e.Name, databaseFileName))
            {
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
    /// Matches only the database file itself, ignoring every other file in the
    /// watched directory: the JSONL export, and the <c>-wal</c>/<c>-shm</c>
    /// siblings a plain read also churns through; see the type-level remarks.
    /// </summary>
    private static bool IsRelevant(string? changedName, string databaseFileName)
        => changedName == databaseFileName;
}
