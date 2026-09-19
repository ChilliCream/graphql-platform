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
    /// Lets the watcher settle after start-up and drains whatever it published
    /// in the meantime. The arrange step writes the main database file before
    /// the watcher exists, and the file system can still deliver that write's
    /// event afterwards; a test asserting that a -wal/-shm-only churn publishes
    /// nothing must not fail on it. Anything published after this returns was
    /// caused by the act step.
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
        // arrange: land the write exactly in the window between the pre-enable
        // -wal baseline read and the watcher raising events, via a hook invoked
        // synchronously at that point, since the window is otherwise too narrow
        // for a test to hit deterministically.
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
        // arrange: land the write exactly in the window between the pre-enable
        // main-file baseline and the watcher raising events, via a hook invoked
        // synchronously at that point, since the window is otherwise too narrow
        // for a test to hit deterministically. No -wal growth is involved, and
        // s_neverFiringDebounce keeps the event-driven path from ever firing, so
        // only the main-file reconciliation can produce the event. Unique among
        // the enable-gap cases: the gap write also grows the file (100 bytes to
        // 150), modeling an ordinary write transaction that both advances the
        // change counter and lengthens the file, so this covers the enable gap
        // and the length signal together, unlike
        // RunAsync_Should_PublishDataChangedEvent_When_MainFileChangeCounterAdvances_WithMtimeAndLengthUnchanged
        // (which forces length and mtime equal to isolate the
        // change-counter term alone), and unlike
        // RunAsync_Should_PublishDataChangedEvent_When_MainFileReplaced_WithSameChangeCounter_ButDifferentLength
        // (which keeps the change counter equal to isolate the
        // length/mtime terms from an unrelated counter difference).
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
        // arrange: a non-SQLite payload, so the watcher falls back to
        // comparing mtime/length (see the MainFileState remarks). The write
        // landing in the enable gap keeps the same length ("initial" and
        // "changed" are both 7 bytes), so only mtime can distinguish them.
        // The mtime is advanced explicitly with File.SetLastWriteTimeUtc
        // rather than left to whatever the wall clock does between two
        // rapid writes, so the fallback path stays covered without
        // depending on file system timestamp granularity, which is exactly
        // the ubuntu-latest risk this test must not reintroduce.
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
        // arrange: a whole-file replacement (a restore from a backup, a copy
        // over the file, a truncation by an external tool) that happens to
        // carry the same SQLite file change counter as the file it replaced.
        // The SqliteDbWatcher type-level remarks say the parent directory is
        // watched precisely so a full file replacement is still caught, so
        // MainFileState.DiffersFrom must not let an equal change counter
        // suppress the length difference this replacement also carries.
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
        // arrange: a real SQLite header so the watcher reads its file change
        // counter (offset 24) instead of comparing mtime/length. The write
        // landing in the enable gap keeps the same length and has its mtime
        // reset back to the baseline value with File.SetLastWriteTimeUtc,
        // reproducing the coarse mtime granularity some Linux file systems
        // exhibit deterministically on every platform: two same-length writes
        // landing within one tick can compare mtime-equal there, which an
        // mtime/length-only compare would silently miss even though a real
        // write transaction advanced the change counter.
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
        // arrange: no OnBaselineCaptured hook lands a write in the enable gap,
        // and s_neverFiringDebounce keeps the event-driven path from firing
        // within the bounded window either, so nothing at all should be
        // published on startup silence. This guards the gap xd8's review
        // found: forcing the reconciliation compare to unconditionally report
        // "changed" still passed this class 14 of 14, because every other
        // test's SettleAsync call drains a spurious startup event instead of
        // asserting its absence.
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
        // arrange: every store connection in this codebase opens without
        // pooling and is disposed after a single query (bd-agent-unify-814.7),
        // so SQLite creates, checkpoints, and deletes the -wal sibling as
        // that connection closes even for a plain read, the same file churn
        // a real write produces on -wal alone. A consumer of
        // DataChangedEvent that itself reads the database (for example a
        // hosted tab's own refresh) must not see its own read echoed back
        // as a fresh change: that would form a self-sustaining refresh loop
        // with no natural quiescence.
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
        // arrange: same self-triggering shape as the -wal sibling above, for
        // the -shm sibling a plain read also churns through.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, s_debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);

        // Simulate a burst of read-triggered -wal/-shm churn (create, modify,
        // delete), the same shape a self-sustaining loop would produce.
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
        // arrange: a real write still lands on the main database file itself
        // (checkpointed back into it as the writing connection closes, same
        // as the read-triggered churn above), so it must still be detected
        // even surrounded by the -wal/-shm noise every connection produces.
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
        // arrange: a second process (or a concurrent TUI reader) holding a read
        // lock makes SQLite skip the close-time checkpoint for a real write, so
        // the frames stay appended in -wal and the main db file's mtime never
        // moves. This is the exact case bd-g3b restores: -wal growth that is
        // never rolled back must still surface a change even though the main
        // db file itself is untouched.
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
        // arrange: a second write landing after the first (still uncheckpointed)
        // one must itself be detected, proving the growth baseline advances
        // instead of only ever comparing against the original empty state.
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
        // arrange: a size-stable rewrite of -wal (touching mtime without
        // appending frames) must stay silent, since only growth is a proxy for
        // an uncheckpointed write; matching the old size is not growth.
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
        // arrange: repeated frame appends to -wal within one debounce window,
        // the shape of a busy uncheckpointed writer, must still coalesce into a
        // single event rather than one per append. The writes are issued
        // back-to-back with no inter-write delay: pacing them via Task.Delay
        // made this reproducibly flaky under load (bd-hai), since a starved
        // thread pool can stretch a "s_debounce / 5" delay past s_debounce itself,
        // letting the timer fire mid-burst and emit a second event. Synchronous
        // writes have no such scheduling dependency and stay well inside the
        // debounce window regardless of system load, which s_burstDebounce widens
        // further so that event delivery alone cannot split the burst either.
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
            // Fires once per debounce cycle. Arrange/SettleAsync activity is
            // excluded via the "acting" flag (set only once the act phase
            // begins below) so a startup tick cannot silently grant the act
            // phase a free extra event. While acting, counts cycles and, on
            // the first act-phase cycle, snapshots how many notifications had
            // been observed by then -- the boundary the tail assertion below
            // measures "late" (post-first-cycle) notifications from.
            OnDebounceTick = () =>
            {
                if (Volatile.Read(ref acting) == 0)
                {
                    return;
                }

                Interlocked.Increment(ref tickCount);
                Interlocked.CompareExchange(ref notificationsAtFirstTick, Volatile.Read(ref notifications), -1);
            },
            // Fires once per raw file system notification for the database or
            // -wal file, before debounce coalesces it. While acting, counts
            // notifications so the tail assertion can tell a legitimate
            // notification that arrived after the first debounce cycle
            // (accounts for an extra publish) from a coalescing defect (an
            // extra publish with no such notification to account for it).
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

        // A burst of writes to the db file within one debounce window resets
        // the same timer rather than each scheduling its own event. Issued
        // back-to-back with no inter-write delay for the same reason as the
        // -wal burst test above (bd-hai): a Task.Delay-paced burst is only as
        // tight as the thread pool's scheduling under load allows. s_burstDebounce
        // widens the window so event delivery alone cannot split the burst either.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath, "changed-" + i);
        }

        var first = await ReadOneAsync(channel.Reader, testToken);

        // This is bd-hai's mechanism: notification delivery jitter under load
        // can split one burst's events into two debounce cycles further apart
        // than the writes themselves, which is a legitimate extra publish,
        // not a coalescing defect. Rather than widen this wait to tolerate
        // it (which would make the assertion blind to a real double-emit),
        // keep the original fixed wait and instead compare what was
        // published against how many notifications actually arrived after
        // the first debounce cycle fired: an extra publish is allowed only
        // when a file system notification was delivered after that first
        // cycle, so legitimate split delivery passes while any publish with
        // no notification to account for it fails.
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

        // assert: the watcher returns promptly on its own rather than only
        // when the timeout delay wins the race, since it never starts a
        // watch loop to wait on.
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
