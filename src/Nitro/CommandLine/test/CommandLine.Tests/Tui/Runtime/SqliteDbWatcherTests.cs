using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class SqliteDbWatcherTests : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The debounce used by the coalescing test. It is far wider than the time the burst takes to
    /// write so that neither file system event delivery nor a scheduler delay stretched by a loaded
    /// machine can push two writes of the same burst into separate debounce windows.
    /// </summary>
    private static readonly TimeSpan BurstDebounce = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

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
        await Task.Delay(Debounce * 4, cancellationToken);

        while (channel.Reader.TryRead(out _))
        {
        }
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(databasePath + "-wal", "wal-bytes");
        File.Delete(databasePath + "-wal");
        await Task.Delay(Debounce * 4, testToken);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
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
            await Task.Delay(Debounce / 5, testToken);
        }

        File.Delete(databasePath + "-shm");
        await Task.Delay(Debounce * 4, testToken);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(walPath, new string('x', 32));
        await Task.Delay(Debounce * 4, testToken);
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
        // thread pool can stretch a "Debounce / 5" delay past Debounce itself,
        // letting the timer fire mid-burst and emit a second event. Synchronous
        // writes have no such scheduling dependency and stay well inside the
        // debounce window regardless of system load, which BurstDebounce widens
        // further so that event delivery alone cannot split the burst either.
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, BurstDebounce);
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
        await Task.Delay(BurstDebounce * 2, testToken);
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
        var watcher = new SqliteDbWatcher(databasePath, BurstDebounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);

        // A burst of writes to the db file within one debounce window resets
        // the same timer rather than each scheduling its own event. Issued
        // back-to-back with no inter-write delay for the same reason as the
        // -wal burst test above (bd-hai): a Task.Delay-paced burst is only as
        // tight as the thread pool's scheduling under load allows. BurstDebounce
        // widens the window so event delivery alone cannot split the burst either.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath, "changed-" + i);
        }

        var first = await ReadOneAsync(channel.Reader, testToken);

        // No further event should follow once the burst settles.
        await Task.Delay(BurstDebounce * 2, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(first);
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunAsync_Should_IgnoreUnrelatedFile_InSameDirectory()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "unrelated");
        await Task.Delay(Debounce * 4, testToken);
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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();

        // act
        var runTask = watcher.RunAsync(channel.Writer, testToken);
        var completed = await Task.WhenAny(runTask, Task.Delay(TestTimeout, testToken));

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
        var watcher = new SqliteDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await SettleAsync(channel, testToken);
        cts.Cancel();
        var completed = await Task.WhenAny(runTask, Task.Delay(TestTimeout, testToken));

        // assert
        Assert.Same(runTask, completed);
        await runTask;
    }

    private static async Task<TuiEvent> ReadOneAsync(ChannelReader<TuiEvent> reader, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TestTimeout);
        return await reader.ReadAsync(cts.Token);
    }
}
