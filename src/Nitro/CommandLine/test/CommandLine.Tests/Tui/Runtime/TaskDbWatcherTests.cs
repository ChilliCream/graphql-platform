using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class TaskDbWatcherTests : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The debounce used by the coalescing test. It is far wider than the time the burst takes to
    /// write so that neither file system event delivery nor a scheduler delay stretched by a loaded
    /// machine can push two writes of the same burst into separate debounce windows.
    /// </summary>
    private static readonly TimeSpan BurstDebounce = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    private readonly string _directory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "task-db-watcher-tests-" + Guid.NewGuid())).FullName;

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
        var watcher = new TaskDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(Debounce, testToken);
        File.WriteAllText(databasePath, "changed");
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_PublishDataChangedEvent_When_WalSiblingWritten()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new TaskDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(Debounce, testToken);
        File.WriteAllText(databasePath + "-wal", "wal-bytes");
        var received = await ReadOneAsync(channel.Reader, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.IsType<TuiEvent.DataChangedEvent>(received);
    }

    [Fact]
    public async Task RunAsync_Should_CoalesceBurstOfWrites_IntoSingleEvent()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(_directory, "tasks.db");
        File.WriteAllText(databasePath, "initial");
        var watcher = new TaskDbWatcher(databasePath, BurstDebounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(Debounce, testToken);

        // A burst of writes to the db and its WAL sibling within one debounce window resets the
        // same timer rather than each write scheduling its own event. The writes are issued back
        // to back so that the burst cannot be stretched across two windows by a delay that a
        // loaded machine overruns.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(databasePath, "changed-" + i);
            File.WriteAllText(databasePath + "-wal", "wal-" + i);
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
        var watcher = new TaskDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(Debounce, testToken);
        File.WriteAllText(Path.Combine(_directory, "tasks.jsonl"), "unrelated");
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
        var watcher = new TaskDbWatcher(databasePath, Debounce);
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
        var watcher = new TaskDbWatcher(databasePath, Debounce);
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = watcher.RunAsync(channel.Writer, cts.Token);
        await Task.Delay(Debounce, testToken);
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
