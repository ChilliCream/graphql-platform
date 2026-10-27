using System.IO.Pipelines;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using static HotChocolate.AspNetCore.Serialization.ResultTestHelper;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class JsonLinesResultFormatterTests
{
    [Fact]
    public async Task FormatAsync_Should_DiscardPartialResult_When_BatchResultFormattingThrows()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        // the stream yields only after the failing result wrote its partial bytes
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batch = new OperationResultBatch(
        [
            CreateStream(gate.Task, CreateResult(new Dictionary<string, object?> { ["s"] = 1 })),
            CreateResult(jsonWriter =>
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("partial");
                gate.SetResult();
                throw new InvalidOperationException("boom");
            })
        ]);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                batch,
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        await writer.CompleteAsync();

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal("{\"data\":{\"s\":1}}\n", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_DiscardPartialResult_When_StreamResultFormattingThrows()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        // the healthy stream yields only after the failing stream wrote its partial bytes
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batch = new OperationResultBatch(
        [
            CreateStream(
                CreateResult(jsonWriter =>
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("partial");
                    gate.SetResult();
                    throw new InvalidOperationException("boom");
                })),
            CreateStream(gate.Task, CreateResult(new Dictionary<string, object?> { ["s2"] = 1 }))
        ]);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                batch,
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        await writer.CompleteAsync();

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal("{\"data\":{\"s2\":1}}\n", Encoding.UTF8.GetString(output.ToArray()));
    }
}
