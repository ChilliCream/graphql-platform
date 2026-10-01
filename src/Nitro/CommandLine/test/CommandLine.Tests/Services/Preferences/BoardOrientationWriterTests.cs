using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Preferences;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tests.Preferences;

public sealed class BoardOrientationWriterTests : IDisposable
{
    private static readonly TimeSpan s_generous = TimeSpan.FromSeconds(10);

    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-board-orientation-writer-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task Enqueue_Should_PersistTheThirdValue_When_ThreeRapidChangesAreMade()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var configDirectory = Path.Combine(_tempRoot.FullName, "config");
        var store = new BoardPreferencesStore(new FileSystem(), new FixedGlobalConfigDirectoryProvider(configDirectory));
        var writer = new BoardOrientationWriter(store);

        // act
        writer.Enqueue(BoardOrientation.SideBySide);
        writer.Enqueue(BoardOrientation.Stacked);
        writer.Enqueue(BoardOrientation.Auto);
        var drained = await writer.DrainAsync(s_generous);

        // assert
        Assert.True(drained);
        Assert.Equal(BoardOrientation.Auto, await store.ReadOrientationAsync(ct));
    }

    [Fact]
    public async Task Enqueue_Should_WriteOneAtATimeAndLetTheLatestValueWin_When_ChangesArriveDuringAWrite()
    {
        // arrange
        var store = new RecordingStore { BlockFirstWrite = true };
        var writer = new BoardOrientationWriter(store);
        writer.Enqueue(BoardOrientation.SideBySide);
        await store.FirstWriteStarted.Task.WaitAsync(s_generous, TestContext.Current.CancellationToken);

        // act
        writer.Enqueue(BoardOrientation.Stacked);
        writer.Enqueue(BoardOrientation.Auto);
        store.ReleaseFirstWrite.SetResult();
        var drained = await writer.DrainAsync(s_generous);

        // assert
        Assert.True(drained);
        Assert.Equal([BoardOrientation.SideBySide, BoardOrientation.Auto], store.Writes);
        Assert.Equal(1, store.MaxConcurrentWrites);
    }

    [Fact]
    public async Task DrainAsync_Should_ReturnTrue_When_NothingWasEnqueued()
    {
        // arrange
        var writer = new BoardOrientationWriter(new RecordingStore());

        // act
        var drained = await writer.DrainAsync(TimeSpan.Zero);

        // assert
        Assert.True(drained);
    }

    [Fact]
    public async Task DrainAsync_Should_ReturnFalseAfterTheTimeout_When_AWriteIsStuck()
    {
        // arrange
        var store = new RecordingStore { BlockFirstWrite = true };
        var writer = new BoardOrientationWriter(store);
        writer.Enqueue(BoardOrientation.Stacked);
        await store.FirstWriteStarted.Task.WaitAsync(s_generous, TestContext.Current.CancellationToken);

        // act
        var drained = await writer.DrainAsync(TimeSpan.FromMilliseconds(50));

        // assert
        Assert.False(drained);
        store.ReleaseFirstWrite.SetResult();
        Assert.True(await writer.DrainAsync(s_generous));
    }

    [Fact]
    public async Task Enqueue_Should_StillWriteTheNextValue_When_AnEarlierWriteFailed()
    {
        // arrange
        var store = new RecordingStore { FailFirstWrite = true };
        var writer = new BoardOrientationWriter(store);

        // act
        writer.Enqueue(BoardOrientation.SideBySide);
        await writer.DrainAsync(s_generous);
        writer.Enqueue(BoardOrientation.Stacked);
        var drained = await writer.DrainAsync(s_generous);

        // assert
        Assert.True(drained);
        Assert.Equal([BoardOrientation.SideBySide, BoardOrientation.Stacked], store.Writes);
    }

    private sealed class RecordingStore : IBoardPreferencesStore
    {
        private readonly object _gate = new();
        private int _concurrentWrites;

        public bool BlockFirstWrite { get; init; }

        public bool FailFirstWrite { get; init; }

        public TaskCompletionSource FirstWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstWrite { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<BoardOrientation> Writes { get; } = [];

        public int MaxConcurrentWrites { get; private set; }

        public Task<BoardOrientation> ReadOrientationAsync(CancellationToken cancellationToken)
            => Task.FromResult(BoardOrientation.Auto);

        public async Task<bool> WriteOrientationAsync(BoardOrientation orientation, CancellationToken cancellationToken)
        {
            bool first;

            lock (_gate)
            {
                _concurrentWrites++;
                MaxConcurrentWrites = Math.Max(MaxConcurrentWrites, _concurrentWrites);
                first = Writes.Count == 0;
                Writes.Add(orientation);
            }

            try
            {
                if (first)
                {
                    FirstWriteStarted.SetResult();

                    if (BlockFirstWrite)
                    {
                        await ReleaseFirstWrite.Task;
                    }
                }

                return !(first && FailFirstWrite);
            }
            finally
            {
                lock (_gate)
                {
                    _concurrentWrites--;
                }
            }
        }
    }

    private sealed class FixedGlobalConfigDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }
}
