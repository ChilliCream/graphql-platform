using System.Buffers.Binary;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class SqliteDbWatcherTests : IDisposable
{
    private static readonly TimeSpan s_debounce = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The debounce used by the coalescing test. It is far wider than the time the burst takes to
    /// write so that neither file system event delivery nor a scheduler delay stretched by a loaded
    /// machine can push two writes of the same burst into separate debounce windows.
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
    /// Waits for the watcher to settle after start-up and drains any events it published in that
    /// window. Anything published after this returns was caused by the act step, not by start-up
    /// reconciliation.
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
        // OnBaselineCaptured fires synchronously during the pre-enable -wal baseline read.
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
        // OnBaselineCaptured grows the file (100 to 150 bytes) and advances the change counter.
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
        // a non-SQLite payload forces the mtime/length fallback; the hook keeps length equal and bumps mtime.
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
        // the replacement keeps the same change counter as the original file but a different length.
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
        // the hook advances the change counter but resets mtime to the baseline and keeps length equal.
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
        // no OnBaselineCaptured hook writes into the enable gap, so no event should publish at all.
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
        // a plain read creates and deletes the -wal sibling too, the same churn a real write leaves on -wal alone.
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
        // a plain read churns through the -shm sibling the same way it churns through -wal above.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);

        // create, modify, and delete -shm repeatedly to model read-triggered churn.
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
        // a real write must still be detected while -wal and -shm noise churns around it.
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
        // a blocked checkpoint leaves the write appended only in -wal, with the main file mtime unmoved.
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
        // a second uncheckpointed write must also be detected, so the growth baseline advances each time.
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
        // a same-size rewrite of -wal touches mtime without appending frames, so it must stay silent.
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
        // five synchronous -wal appends within one debounce window must coalesce into a single event.
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
            // fires once per debounce cycle, counted only once the act phase sets the "acting" flag.
            OnDebounceTick = () =>
            {
                if (Volatile.Read(ref acting) == 0)
                {
                    return;
                }

                Interlocked.Increment(ref tickCount);
                Interlocked.CompareExchange(ref notificationsAtFirstTick, Volatile.Read(ref notifications), -1);
            },
            // fires once per raw file system notification, before debounce coalesces it.
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

        // five synchronous writes within one debounce window reset the same timer instead of firing separately.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath, "changed-" + i);
        }

        var first = await ReadOneAsync(channel.Reader, testToken);

        // an extra publish is allowed only when a notification arrived after the first debounce cycle.
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
        // the watcher returns on its own rather than only when the timeout delay wins the race.
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
