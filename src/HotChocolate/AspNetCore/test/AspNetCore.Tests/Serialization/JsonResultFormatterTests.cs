using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class JsonResultFormatterTests
{
    [Fact]
    public async Task FormatAsync_Should_WriteResultInOneChunk_When_ResultFitsIntoChunk()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var result = new OperationResult(
            CreateData(
                new Dictionary<string, object?>
                {
                    ["product"] = new Dictionary<string, object?>
                    {
                        ["name"] = "Abc",
                        ["price"] = 12.5
                    }
                }));

        // act
        await JsonResultFormatter.Default.FormatAsync(
            result,
            writer,
            ExecutionResultFormatFlags.None,
            CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        Assert.Equal(
            """{"data":{"product":{"name":"Abc","price":12.5}}}""",
            Encoding.UTF8.GetString(output.ToArray()));
        Assert.Equal(1, writer.SpanCount);
        Assert.Equal(1, writer.AdvanceCount);
        Assert.Equal(1, writer.FlushCount);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteCompleteResult_When_ResultExceedsChunkSize()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var items = new List<object?>();

        for (var i = 0; i < 4000; i++)
        {
            items.Add(new Dictionary<string, object?> { ["id"] = i, ["name"] = $"item-{i}-{new string('x', 40)}" });
        }

        var data = new Dictionary<string, object?> { ["items"] = items };
        var result = new OperationResult(CreateData(data));
        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(new OperationResult(CreateData(data)), expected);

        // act
        await JsonResultFormatter.Default.FormatAsync(
            result,
            writer,
            ExecutionResultFormatFlags.None,
            CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        Assert.True(expected.WrittenCount > 64 * 1024);
        Assert.Equal(expected.WrittenSpan.ToArray(), output.ToArray());
        Assert.Equal(1, writer.FlushCount);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteScalarThroughPipe_When_ScalarExceedsChunkSize()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var text = new string('x', 1024 * 1024);
        var data = new Dictionary<string, object?> { ["text"] = text, ["after"] = "tail" };
        var result = new OperationResult(CreateData(data));
        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(new OperationResult(CreateData(data)), expected);

        // act
        await JsonResultFormatter.Default.FormatAsync(
            result,
            writer,
            ExecutionResultFormatFlags.None,
            CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        Assert.Equal(expected.WrittenSpan.ToArray(), output.ToArray());
        Assert.Equal(text.Length * 3 + 3, writer.MaxSizeHint);
        Assert.Equal(1, writer.FlushCount);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteBatchWithOneFlush_When_ResultIsBatch()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var batch = new OperationResultBatch(
        [
            new OperationResult(CreateData(new Dictionary<string, object?> { ["a"] = 1 })),
            new OperationResult(CreateData(new Dictionary<string, object?> { ["b"] = 2 }))
        ]);

        // act
        await JsonResultFormatter.Default.FormatAsync(
            batch,
            writer,
            ExecutionResultFormatFlags.None,
            CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        Assert.Equal("""{"data":{"a":1}}{"data":{"b":2}}""", Encoding.UTF8.GetString(output.ToArray()));
        Assert.Equal(1, writer.SpanCount);
        Assert.Equal(1, writer.FlushCount);
    }

    [Fact]
    public async Task FormatAsync_Should_FlushPerResult_When_ResultIsStream()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var stream = new ResponseStream(
            () => CreateResults(
            [
                new OperationResult(CreateData(new Dictionary<string, object?> { ["a"] = 1 })),
                new OperationResult(CreateData(new Dictionary<string, object?> { ["b"] = 2 }))
            ]),
            ExecutionResultKind.SubscriptionResult);

        // act
        await JsonResultFormatter.Default.FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.None,
            CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        Assert.Equal("""{"data":{"a":1}}{"data":{"b":2}}""", Encoding.UTF8.GetString(output.ToArray()));
        Assert.Equal(2, writer.SpanCount);
        Assert.Equal(2, writer.FlushCount);
    }

    private static async IAsyncEnumerable<OperationResult> CreateResults(IReadOnlyList<OperationResult> results)
    {
        foreach (var result in results)
        {
            yield return result;
            await Task.Yield();
        }
    }

    private static OperationResultData CreateData(object value)
        => new(value, isValueNull: false, new DictionaryJsonFormatter(value), memoryHolder: null);

    private sealed class DictionaryJsonFormatter(object value) : IRawJsonFormatter
    {
        private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

        public void WriteDataTo(JsonWriter jsonWriter)
            => JsonValueFormatter.WriteValue(jsonWriter, value, s_options);
    }
}
