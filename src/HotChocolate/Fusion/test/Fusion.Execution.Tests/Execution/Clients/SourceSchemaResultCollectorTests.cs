using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaResultCollectorTests
{
    private static readonly byte[] s_payload = """{"data":{"field":"value"}}"""u8.ToArray();

    [Fact]
    public void OnResult_Should_HoldResultWithoutBuffer_When_OneResultArrives()
    {
        // arrange
        using var arena = new MemoryArena();
        var collector = new SourceSchemaResultCollector(initialCapacity: 2);
        var result = CreateResult(arena);

        // act
        collector.OnResult(0, result);

        // assert
        Assert.Same(result, collector.Single);
        Assert.Null(collector.Buffer);
        collector.DisposeResults();
    }

    [Fact]
    public void OnResult_Should_KeepEveryResultInOrder_When_ResultsExceedRentedBuffer()
    {
        // arrange
        using var arena = new MemoryArena();
        var collector = new SourceSchemaResultCollector(initialCapacity: 2);
        var results = Enumerable.Range(0, 40).Select(_ => CreateResult(arena)).ToArray();

        // act
        foreach (var result in results)
        {
            collector.OnResult(0, result);
        }

        // assert
        Assert.Equal(results, collector.Buffer!.AsSpan(0, collector.Count).ToArray());
        collector.DisposeResults();
    }

    [Fact]
    public void DisposeResults_Should_DisposeEveryResultAndReset_When_CalledTwice()
    {
        // arrange
        using var arena = new MemoryArena();
        var collector = new SourceSchemaResultCollector(initialCapacity: 2);
        var documents = new List<SourceResultDocument>();

        for (var i = 0; i < 3; i++)
        {
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            documents.Add(document);
            collector.OnResult(0, new SourceSchemaResult(CompactPath.Root, document));
        }

        // act
        collector.DisposeResults();
        collector.DisposeResults();

        // assert
        new
        {
            DocumentExceptions = documents
                .Select(d => Record.Exception(() => d.Root.GetProperty("data"))?.GetType().Name)
                .ToArray(),
            collector.Count,
            HasSingle = collector.Single is not null,
            HasBuffer = collector.Buffer is not null
        }.MatchInlineSnapshot(
            """
            {
              "DocumentExceptions": [
                "ObjectDisposedException",
                "ObjectDisposedException",
                "ObjectDisposedException"
              ],
              "Count": 0,
              "HasSingle": false,
              "HasBuffer": false
            }
            """);
    }

    private static SourceSchemaResult CreateResult(MemoryArena arena)
        => new(CompactPath.Root, SourceResultDocument.Parse(arena, s_payload, s_payload.Length));
}
