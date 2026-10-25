using System.IO.Pipelines;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using static HotChocolate.AspNetCore.Serialization.ResultTestHelper;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class EventStreamResultFormatterTests
{
    [Fact]
    public async Task FormatAsync_Should_DiscardPartialMessage_When_BatchResultFormattingThrows()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(jsonWriter =>
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("partial");
                throw new InvalidOperationException("boom");
            }),
            CreateResult(new Dictionary<string, object?> { ["c"] = 3 })
        ]);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new EventStreamResultFormatter(default).FormatAsync(
                batch,
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        await writer.CompleteAsync();

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal(
            "event: next\ndata: {\"data\":{\"a\":1}}\n\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_DiscardPartialMessage_When_StreamResultFormattingThrows()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        // the healthy stream yields only after the failing stream wrote its partial message
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
            async () => await new EventStreamResultFormatter(default).FormatAsync(
                batch,
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        await writer.CompleteAsync();

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal(
            "event: next\ndata: {\"data\":{\"s2\":1}}\n\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }
}
