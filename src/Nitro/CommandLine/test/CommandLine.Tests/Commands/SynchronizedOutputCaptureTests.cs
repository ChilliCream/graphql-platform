namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

public sealed class SynchronizedOutputCaptureTests
{
    [Fact]
    public async Task Snapshot_Should_PreserveCompleteAppends_When_AppendsAndSnapshotsAreConcurrent()
    {
        // arrange
        const int writerCount = 8;
        const int chunksPerWriter = 16;
        const int snapshotterCount = 8;
        var capture = new SynchronizedOutputCapture();
        var chunks = Enumerable.Range(0, writerCount * chunksPerWriter)
            .Select(index => $"{index:D3}:{new string((char)('A' + index % 26), 64)}|")
            .ToArray();
        var expectedChunks = chunks.Select(chunk => chunk[..^1]).Order().ToArray();

        // act
        var results = await ConcurrentTestHarness.RunAsync(
            writerCount + snapshotterCount,
            async caller =>
            {
                if (caller <= writerCount)
                {
                    var start = (caller - 1) * chunksPerWriter;

                    for (var index = start; index < start + chunksPerWriter; index++)
                    {
                        capture.Writer.Write(chunks[index]);
                        await Task.Yield();
                    }

                    return [];
                }

                var snapshots = new string[chunksPerWriter];

                for (var index = 0; index < snapshots.Length; index++)
                {
                    snapshots[index] = capture.GetOutput();
                    await Task.Yield();
                }

                return snapshots;
            });
        var snapshots = results.Skip(writerCount).SelectMany(result => result);
        var output = capture.GetOutput();

        // assert
        Assert.All(
            snapshots,
            snapshot => Assert.All(
                snapshot.Split('|', StringSplitOptions.RemoveEmptyEntries),
                chunk => Assert.Contains(chunk, expectedChunks)));
        Assert.Equal(
            expectedChunks,
            output.Split('|', StringSplitOptions.RemoveEmptyEntries).Order());
    }
}
