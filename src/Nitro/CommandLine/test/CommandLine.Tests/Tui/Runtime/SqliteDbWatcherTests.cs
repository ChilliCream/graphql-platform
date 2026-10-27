using System.Buffers.Binary;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class SqliteDbWatcherTests : IDisposable
{
    private static readonly TimeSpan s_debounce = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The debounce interval used by burst-coalescing tests.
    /// </summary>
    private static readonly TimeSpan s_burstDebounce = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The delay <see cref="SettleAsync"/> waits between drain passes while checking for
    /// quiescence.
    /// </summary>
    private static readonly TimeSpan s_settleDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The maximum time a test waits for a single expected event, and the upper bound on
    /// <see cref="SettleAsync"/>'s quiescence loop.
    /// </summary>
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// A debounce far wider than <see cref="s_testTimeout"/>, so an event-driven emit (which
    /// always waits out the debounce timer first) cannot land inside the test's read window,
    /// leaving only the synchronous post-enable reconciliation able to do so.
    /// </summary>
    private static readonly TimeSpan s_neverFiringDebounce = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Drains events after each quiet-period delay until a pass finds none, with a bounded timeout.
    /// Startup notifications may still arrive after this method returns.
    /// </summary>
    private static async Task SettleAsync(Channel<TuiEvent> channel, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(s_testTimeout);

        bool sawEvent;

        do
        {
            await Task.Delay(s_settleDelay, cts.Token);

            sawEvent = false;

            while (channel.Reader.TryRead(out _))
            {
                sawEvent = true;
            }
        }
        while (sawEvent);
    }

    private readonly string _directory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "sqlite-db-watcher-tests-" + Guid.NewGuid())).FullName;

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_DatabaseFileWritten()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath, "changed");
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_WalGrowsBeforeEventsAreEnabled()
    {
        // arrange
        // The hook writes after the baseline is captured and before file events are enabled.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var walPath = databasePath + "-wal";
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce)
        {
            OnBaselineCaptured = () => File.WriteAllText(walPath, new string('w', 64))
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_DatabaseFileWrittenBeforeEventsAreEnabled()
    {
        // arrange
        // The hook grows the file from 100 to 150 bytes and advances its change counter.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllBytes(databasePath, CreateSqliteHeader(changeCounter: 1));
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce)
        {
            OnBaselineCaptured = () =>
            {
                var grown = new byte[150];
                CreateSqliteHeader(changeCounter: 2).CopyTo(grown, 0);
                File.WriteAllBytes(databasePath, grown);
            }
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_NonSqliteMainFileMtimeAdvances_BeforeEventsAreEnabled()
    {
        // arrange
        // The hook changes a non-SQLite file, keeping its length and advancing its modification time.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var baselineWriteTimeUtc = File.GetLastWriteTimeUtc(databasePath);
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce)
        {
            OnBaselineCaptured = () =>
            {
                File.WriteAllText(databasePath, "changed");
                File.SetLastWriteTimeUtc(databasePath, baselineWriteTimeUtc + TimeSpan.FromSeconds(1));
            }
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_MainFileReplaced_WithSameChangeCounter_ButDifferentLength()
    {
        // arrange
        // The replacement preserves the change counter and changes the file length.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllBytes(databasePath, CreateSqliteHeader(changeCounter: 5));
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce)
        {
            OnBaselineCaptured = () =>
            {
                var replacement = new byte[150];
                CreateSqliteHeader(changeCounter: 5).CopyTo(replacement, 0);
                File.WriteAllBytes(databasePath, replacement);
            }
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_MainFileChangeCounterAdvances_WithMtimeAndLengthUnchanged()
    {
        // arrange
        // Only the change counter advances; file length and modification time remain unchanged.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllBytes(databasePath, CreateSqliteHeader(changeCounter: 1));
        var baselineWriteTimeUtc = File.GetLastWriteTimeUtc(databasePath);
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce)
        {
            OnBaselineCaptured = () =>
            {
                File.WriteAllBytes(databasePath, CreateSqliteHeader(changeCounter: 2));
                File.SetLastWriteTimeUtc(databasePath, baselineWriteTimeUtc);
            }
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_NotPublishDataChangedEvent_When_NothingWrittenAtStartup()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_neverFiringDebounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(s_settleDelay, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_NotPublishDataChangedEvent_When_OnlyWalSiblingWritten()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath + "-wal", "wal-bytes");
        File.Delete(databasePath + "-wal");
        await Task.Delay(s_debounce * 4, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_NotPublishDataChangedEvent_When_OnlyShmSiblingWritten()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);

        // Create and rewrite the shared-memory file before deleting it.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath + "-shm", "shm-" + i);
            await Task.Delay(s_debounce / 5, testToken);
        }

        File.Delete(databasePath + "-shm");
        await Task.Delay(s_debounce * 4, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_ForARealWrite_AmidWalAndShmChurn()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath + "-wal", "wal-bytes");
        File.WriteAllText(databasePath + "-shm", "shm-bytes");
        File.WriteAllText(databasePath, "changed");
        File.Delete(databasePath + "-wal");
        File.Delete(databasePath + "-shm");
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_WalGrowsAndStaysGrown_LikeACheckpointBlockedByAConcurrentReader()
    {
        // arrange
        // Only the WAL file grows; the main database file remains unchanged.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath + "-wal", new string('w', 64));
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_WalGrowsPastAPriorGrowth()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath + "-wal", new string('w', 32));
        var first = await ReadOneAsync(channel.Reader, testToken);
        File.WriteAllText(databasePath + "-wal", new string('w', 96));
        var second = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(first);
        Assert.IsType<TuiEvent.DataChangedEvent>(second);
    }

    [Fact]
    public async Task RunAsync_Should_NotPublishDataChangedEvent_When_WalIsRewrittenAtTheSameSize()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var walPath = databasePath + "-wal";
        File.WriteAllText(walPath, new string('w', 32));
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(walPath, new string('x', 32));
        await Task.Delay(s_debounce * 4, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_CoalesceBurstOfWalGrowth_IntoSingleEvent()
    {
        // arrange
        // Write five successively larger WAL files without delays between writes.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_burstDebounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);

        for (var i = 1; i <= 5; i++)
        {
            File.WriteAllText(databasePath + "-wal", new string('w', i * 16));
        }

        var first = await ReadOneAsync(channel.Reader, testToken);

        // No further event should follow once the burst settles.
        await Task.Delay(s_burstDebounce * 2, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(first);
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_CoalesceBurstOfWrites_IntoSingleEvent()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var acting = 0;
        var tickCount = 0;
        var notifications = 0;
        var notificationsAtFirstTick = -1;
        var watcher = new SqliteDbWatcher(databasePath, s_burstDebounce)
        {
            // Count act-phase debounce callbacks and capture the notification count at the first one.
            OnDebounceTick = () =>
            {
                if (Volatile.Read(ref acting) == 0)
                {
                    return;
                }

                Interlocked.Increment(ref tickCount);
                Interlocked.CompareExchange(ref notificationsAtFirstTick, Volatile.Read(ref notifications), -1);
            },
            // Count database and WAL notifications after each one rearms the debounce timer.
            OnNotificationObserved = () =>
            {
                if (Volatile.Read(ref acting) != 0)
                {
                    Interlocked.Increment(ref notifications);
                }
            }
        };
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        Volatile.Write(ref acting, 1);

        // Write five database updates without delays between writes.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath, "changed-" + i);
        }

        var first = await ReadOneAsync(channel.Reader, testToken);

        // Each additional event requires a notification received after the first debounce callback.
        await Task.Delay(s_burstDebounce * 2, testToken);
        cts.Cancel();
        await runTask;

        var extraEvents = 0;

        while (channel.Reader.TryRead(out _))
        {
            extraEvents++;
        }

        var notificationsAtFirstTickSnapshot = Volatile.Read(ref notificationsAtFirstTick);

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(first);
        Assert.True(
            notificationsAtFirstTickSnapshot >= 0,
            "no act-phase debounce cycle was observed, so the tail assertion has no boundary to "
            + "measure late notifications from and cannot discriminate a coalescing defect");

        var lateNotifications = Volatile.Read(ref notifications) - notificationsAtFirstTickSnapshot;

        Assert.True(
            extraEvents <= lateNotifications,
            $"published {1 + extraEvents} events for {tickCount} debounce cycles with {lateNotifications} notifications delivered after the first cycle");
    }

    [Fact]
    public async Task RunAsync_Should_IgnoreUnrelatedFile_InSameDirectory()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "unrelated");
        await Task.Delay(s_debounce * 4, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_CompleteWithoutThrowing_When_ParentDirectoryMissing()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "does-not-exist", "tasks.db");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();

        // act
        var runTask = watcher.RunAsync(channel.Writer, testToken);
        var completed = await Task.WhenAny(runTask, Task.Delay(s_testTimeout, testToken));

        // assert
        Assert.Same(runTask, completed);
        await runTask;
    }

    [Fact]
    public async Task RunAsync_Should_StopAndComplete_When_Cancelled()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        cts.Cancel();
        var completed = await Task.WhenAny(runTask, Task.Delay(s_testTimeout, testToken));

        // assert
        Assert.Same(runTask, completed);
        await runTask;
    }

    private static async Task<TuiEvent> ReadOneAsync(ChannelReader<TuiEvent> reader, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(s_testTimeout);
        return await reader.ReadAsync(cts.Token);
    }

    /// <summary>
    /// Builds a minimal, exactly header-sized SQLite database file: the fixed
    /// 16-byte magic string followed by zeroed header fields except the 4-byte
    /// big-endian file change counter at offset 24, matching the layout
    /// <c>SqliteDbWatcher</c> reads. Every returned buffer has the same
    /// length, so two calls with different counters model a real same-length
    /// write transaction.
    /// </summary>
    private static byte[] CreateSqliteHeader(uint changeCounter)
    {
        var header = new byte[100];
        "SQLite format 3\0"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(24, 4), changeCounter);
        return header;
    }
}
