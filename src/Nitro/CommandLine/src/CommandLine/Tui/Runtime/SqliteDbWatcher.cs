using System.Buffers.Binary;
using System.Threading.Channels;

namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// A <see cref="TuiEventSource"/> that watches an agent workspace's SQLite database
/// file and publishes a debounced <see cref="TuiEvent.DataChangedEvent"/> whenever
/// the on-disk data changes, ignoring churn from this process's own reads.
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
    /// Invoked synchronously as the first statement of <c>OnTick</c>, once per
    /// debounce cycle. Test-only seam, a no-op unless a caller sets it.
    /// </summary>
    internal Action? OnDebounceTick { get; init; }

    /// <summary>
    /// Invoked synchronously in <c>OnEvent</c>, immediately after the debounce
    /// timer is (re)armed for a raw file system notification on the database
    /// or <c>-wal</c> file. Test-only seam, a no-op unless a caller sets it.
    /// </summary>
    internal Action? OnNotificationObserved { get; init; }

    /// <summary>
    /// Watches the database file until <paramref name="cancellationToken"/> is
    /// cancelled. When the parent directory does not exist or the file system does
    /// not support watching it, this returns without writing anything, so the
    /// caller degrades silently to manual refresh. A write landing after this
    /// method captures its baseline but before the watcher starts raising events
    /// is still reconciled once it does.
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

        var lastWalSize = GetFileSize(walPath);

        // Reconciliation baseline, captured before OnBaselineCaptured fires so a
        // test can land a write deterministically inside the enable gap.
        var lastMainState = GetMainFileState(_databasePath);
        var mainDatabaseChanged = false;
        var walChanged = false;

        OnBaselineCaptured?.Invoke();

        void OnTick(object? _)
        {
            OnDebounceTick?.Invoke();

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
                OnNotificationObserved?.Invoke();
            }
            else if (e.Name == walFileName)
            {
                walChanged = true;
                timer.Change(_debounce, Timeout.InfiniteTimeSpan);
                OnNotificationObserved?.Invoke();
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

            // Closes the gap between the pre-enable baseline and the watcher
            // actually raising events. A duplicate DataChangedEvent is harmless.
            var reconciledMainState = GetMainFileState(_databasePath);
            var reconciledWalSize = GetFileSize(walPath);

            if (reconciledMainState.DiffersFrom(lastMainState) || reconciledWalSize > lastWalSize)
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
    /// The size, in bytes, of the SQLite database header this type reads to
    /// obtain the file change counter.
    /// </summary>
    private const int SqliteHeaderSize = 100;

    /// <summary>
    /// The byte offset, within the SQLite database header, of the 4-byte
    /// big-endian file change counter that SQLite increments on every write
    /// transaction.
    /// </summary>
    private const int ChangeCounterOffset = 24;

    /// <summary>
    /// The fixed 16-byte magic string every SQLite database file begins with.
    /// </summary>
    private static ReadOnlySpan<byte> SqliteMagic => "SQLite format 3\0"u8;

    /// <summary>
    /// A comparable snapshot of the main database file, used to detect a write
    /// that lands after <see cref="RunAsync"/> captures its baseline but before
    /// the underlying watcher starts raising events.
    /// </summary>
    private readonly record struct MainFileState(
        bool HasChangeCounter,
        uint ChangeCounter,
        DateTime LastWriteTimeUtc,
        long Length)
    {
        public static readonly MainFileState Absent = new(false, 0, DateTime.MinValue, 0);

        public bool DiffersFrom(MainFileState other) =>
            (HasChangeCounter && other.HasChangeCounter && ChangeCounter != other.ChangeCounter)
                || LastWriteTimeUtc != other.LastWriteTimeUtc
                || Length != other.Length;
    }

    /// <summary>
    /// Returns a <see cref="MainFileState"/> snapshot of the file at
    /// <paramref name="path"/>, or <see cref="MainFileState.Absent"/> when it
    /// does not exist or cannot be read.
    /// </summary>
    private static MainFileState GetMainFileState(string path)
    {
        try
        {
            var info = new FileInfo(path);

            if (!info.Exists)
            {
                return MainFileState.Absent;
            }

            var lastWriteTimeUtc = info.LastWriteTimeUtc;
            var length = info.Length;

            if (length >= SqliteHeaderSize && TryReadChangeCounter(path, out var changeCounter))
            {
                return new MainFileState(true, changeCounter, lastWriteTimeUtc, length);
            }

            return new MainFileState(false, 0, lastWriteTimeUtc, length);
        }
        catch (IOException)
        {
            return MainFileState.Absent;
        }
    }

    /// <summary>
    /// Reads the SQLite database header from the file at <paramref name="path"/>
    /// and, when the magic string matches, returns its file change counter.
    /// Returns <see langword="false"/> for anything that is not a readable
    /// SQLite database header, leaving the caller to fall back to mtime/length.
    /// </summary>
    private static bool TryReadChangeCounter(string path, out uint changeCounter)
    {
        changeCounter = 0;

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> header = stackalloc byte[SqliteHeaderSize];
            var totalRead = 0;

            while (totalRead < header.Length)
            {
                var read = stream.Read(header[totalRead..]);

                if (read == 0)
                {
                    return false;
                }

                totalRead += read;
            }

            if (!header[..SqliteMagic.Length].SequenceEqual(SqliteMagic))
            {
                return false;
            }

            changeCounter = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(ChangeCounterOffset, sizeof(uint)));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
