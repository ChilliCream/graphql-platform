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

    [Fact]
    public async Task FormatAsync_Should_FlushOnce_When_BatchResultsAreReady()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(new Dictionary<string, object?> { ["b"] = 2 }),
            CreateResult(new Dictionary<string, object?> { ["c"] = 3 })
        ]);

        // act
        await new JsonLinesResultFormatter(default).FormatAsync(
            batch,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        Assert.Equal(1, writer.FlushCount);
        Assert.Equal(
            "{\"data\":{\"a\":1}}\n{\"data\":{\"b\":2}}\n{\"data\":{\"c\":3}}\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_FlushBatchResults_When_UnflushedBytesReachThreshold()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        // the first two results reach the threshold together
        var value = new string('a', JsonLinesResultFormatter.FlushThreshold / 2);
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = value }),
            CreateResult(new Dictionary<string, object?> { ["b"] = value }),
            CreateResult(new Dictionary<string, object?> { ["c"] = value })
        ]);
        var expected = string.Concat(
            $"{{\"data\":{{\"a\":\"{value}\"}}}}\n",
            $"{{\"data\":{{\"b\":\"{value}\"}}}}\n",
            $"{{\"data\":{{\"c\":\"{value}\"}}}}\n");

        // act
        await new JsonLinesResultFormatter(default).FormatAsync(
            batch,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        // one flush for the first two results and one for the third
        Assert.Equal(2, writer.FlushCount);
        Assert.Equal(expected, Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_SendEarlierResults_When_ReadyBatchResultFormattingThrows()
    {
        // arrange
        var pipe = new Pipe();
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(jsonWriter =>
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("partial");
                throw new InvalidOperationException("boom");
            })
        ]);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                batch,
                pipe.Writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        // the pipe is not completed, so the reader only sees what was flushed
        var flushed = pipe.Reader.TryRead(out var read) ? Encoding.UTF8.GetString(read.Buffer) : null;

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal("{\"data\":{\"a\":1}}\n", flushed);
    }

    [Fact]
    public async Task FormatAsync_Should_FlushBatchResultsBeforeWaiting_When_BatchStreamIsNotReady()
    {
        // arrange
        var pipe = new Pipe();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateStream(gate.Task, CreateResult(new Dictionary<string, object?> { ["s"] = 2 })),
            CreateResult(new Dictionary<string, object?> { ["b"] = 3 })
        ]);

        // act
        // the formatter runs until it waits for the stream of the batch
        var format = new JsonLinesResultFormatter(default)
            .FormatAsync(
                batch,
                pipe.Writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken)
            .AsTask();
        string? flushedBeforeGate = null;

        if (pipe.Reader.TryRead(out var read))
        {
            flushedBeforeGate = Encoding.UTF8.GetString(read.Buffer);
            pipe.Reader.AdvanceTo(read.Buffer.End);
        }

        gate.SetResult();
        await format;
        await pipe.Writer.CompleteAsync();
        read = await pipe.Reader.ReadAsync(TestContext.Current.CancellationToken);
        var flushedAfterGate = Encoding.UTF8.GetString(read.Buffer);

        // assert
        Assert.Equal("{\"data\":{\"a\":1}}\n{\"data\":{\"b\":3}}\n", flushedBeforeGate);
        Assert.Equal("{\"data\":{\"s\":2}}\n", flushedAfterGate);
    }

    [Fact]
    public async Task FormatAsync_Should_FlushResultBeforeWaiting_When_NextStreamResultIsNotReady()
    {
        // arrange
        var pipe = new Pipe();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = new ResponseStream(CreateResults, ExecutionResultKind.DeferredResult);

        async IAsyncEnumerable<OperationResult> CreateResults()
        {
            yield return CreateResult(new Dictionary<string, object?> { ["a"] = 1 });
            await gate.Task;
            yield return CreateResult(new Dictionary<string, object?> { ["b"] = 2 });
        }

        // act
        // the formatter runs until the stream waits for the gate
        var format = new JsonLinesResultFormatter(default)
            .FormatAsync(
                stream,
                pipe.Writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken)
            .AsTask();
        string? flushedBeforeGate = null;

        if (pipe.Reader.TryRead(out var read))
        {
            flushedBeforeGate = Encoding.UTF8.GetString(read.Buffer);
            pipe.Reader.AdvanceTo(read.Buffer.End);
        }

        gate.SetResult();
        await format;
        await pipe.Writer.CompleteAsync();
        read = await pipe.Reader.ReadAsync(TestContext.Current.CancellationToken);
        var flushedAfterGate = Encoding.UTF8.GetString(read.Buffer);

        // assert
        Assert.Equal("{\"data\":{\"a\":1}}\n", flushedBeforeGate);
        Assert.Equal("{\"data\":{\"b\":2}}\n", flushedAfterGate);
    }

    [Fact]
    public async Task FormatAsync_Should_FlushOnce_When_StreamResultsAreReadyTogether()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var stream = CreateReadyStream(
            ExecutionResultKind.BatchResult,
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(new Dictionary<string, object?> { ["b"] = 2 }),
            CreateResult(new Dictionary<string, object?> { ["c"] = 3 }));

        // act
        await new JsonLinesResultFormatter(default).FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        // one flush for the three results and one when the stream has ended
        Assert.Equal(2, writer.FlushCount);
        Assert.Equal(
            "{\"data\":{\"a\":1}}\n{\"data\":{\"b\":2}}\n{\"data\":{\"c\":3}}\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_FlushReadyStreamResults_When_UnflushedBytesReachThreshold()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        // the first two results reach the threshold together
        var value = new string('a', JsonLinesResultFormatter.FlushThreshold / 2);
        var stream = CreateReadyStream(
            ExecutionResultKind.BatchResult,
            CreateResult(new Dictionary<string, object?> { ["a"] = value }),
            CreateResult(new Dictionary<string, object?> { ["b"] = value }),
            CreateResult(new Dictionary<string, object?> { ["c"] = value }));
        var expected = string.Concat(
            $"{{\"data\":{{\"a\":\"{value}\"}}}}\n",
            $"{{\"data\":{{\"b\":\"{value}\"}}}}\n",
            $"{{\"data\":{{\"c\":\"{value}\"}}}}\n");

        // act
        await new JsonLinesResultFormatter(default).FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        // one flush for the first two results, one for the third and one when the stream has ended
        Assert.Equal(3, writer.FlushCount);
        Assert.Equal(expected, Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_SendEarlierResults_When_ReadyStreamResultFormattingThrows()
    {
        // arrange
        var pipe = new Pipe();
        var stream = CreateReadyStream(
            ExecutionResultKind.BatchResult,
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(jsonWriter =>
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("partial");
                throw new InvalidOperationException("boom");
            }));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                stream,
                pipe.Writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken));
        // the pipe is not completed, so the reader only sees what was flushed
        var flushed = pipe.Reader.TryRead(out var read) ? Encoding.UTF8.GetString(read.Buffer) : null;

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.Equal("{\"data\":{\"a\":1}}\n", flushed);
    }

    [Fact]
    public async Task FormatAsync_Should_ThrowFlushError_When_FlushFailsWhileStreamMovesToNextResult()
    {
        // arrange
        await using var output = new MemoryStream();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        // the failing write cancels the request, which ends the pending move of the stream
        var writer = new FailingPipeWriter(
            PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)),
            cts.Cancel);
        var results = new ManualResultStream(CreateResult(new Dictionary<string, object?> { ["a"] = 1 }))
        {
            HonorsCancellation = true
        };

        // act
        var exception = await Assert.ThrowsAsync<IOException>(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                new ResponseStream(() => results, ExecutionResultKind.BatchResult),
                writer,
                ExecutionResultFormatFlags.None,
                cts.Token));

        // assert
        Assert.Equal(FailingPipeWriter.Message, exception.Message);
        Assert.Equal(1, results.DisposeCount);
    }

    [Fact]
    public async Task FormatAsync_Should_DropNextResult_When_FlushFailedBeforeStreamYieldsIt()
    {
        // arrange
        await using var output = new MemoryStream();
        var inner = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var writer = new FailingPipeWriter(inner, () => { });
        var results = new ManualResultStream(CreateResult(new Dictionary<string, object?> { ["a"] = 1 }));
        var second = CreateResult(new Dictionary<string, object?> { ["b"] = 2 });
        var isSecondDisposed = false;
        second.RegisterForCleanup(() => isSecondDisposed = true);

        // act
        // the formatter waits for the pending move of the stream after its flush failed
        var format = new JsonLinesResultFormatter(default)
            .FormatAsync(
                new ResponseStream(() => results, ExecutionResultKind.BatchResult),
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken)
            .AsTask();
        results.Yield(second);
        var exception = await Assert.ThrowsAsync<IOException>(() => format);
        await inner.CompleteAsync();

        // assert
        Assert.Equal(FailingPipeWriter.Message, exception.Message);
        Assert.Equal("{\"data\":{\"a\":1}}\n", Encoding.UTF8.GetString(output.ToArray()));
        Assert.True(isSecondDisposed);
        Assert.Equal(1, results.DisposeCount);
    }

    [Fact]
    public async Task FormatAsync_Should_CompleteWithoutError_When_CanceledWhileStreamResultIsBuffered()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        // the request is canceled after the first result was written and before it is flushed
        var results = new ManualResultStream(CreateResult(new Dictionary<string, object?> { ["a"] = 1 }))
        {
            HonorsCancellation = true,
            OnSecondMove = cts.Cancel
        };

        // act
        var exception = await Record.ExceptionAsync(
            async () => await new JsonLinesResultFormatter(default).FormatAsync(
                new ResponseStream(() => results, ExecutionResultKind.BatchResult),
                writer,
                ExecutionResultFormatFlags.None,
                cts.Token));

        // assert
        Assert.Null(exception);
        Assert.Equal(1, results.DisposeCount);
    }

    [Fact]
    public async Task FormatAsync_Should_FlushEachResult_When_SubscriptionResultsAreReadyTogether()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var stream = CreateReadyStream(
            ExecutionResultKind.SubscriptionResult,
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateResult(new Dictionary<string, object?> { ["b"] = 2 }),
            CreateResult(new Dictionary<string, object?> { ["c"] = 3 }));

        // act
        await new JsonLinesResultFormatter(default).FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        // one flush for each of the three results and one when the stream has ended
        Assert.Equal(4, writer.FlushCount);
        Assert.Equal(
            "{\"data\":{\"a\":1}}\n{\"data\":{\"b\":2}}\n{\"data\":{\"c\":3}}\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task FormatAsync_Should_FlushResultBeforeNextMove_When_StreamIsSubscription()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var flushesBeforeSecondMove = -1;
        var results = new ManualResultStream(CreateResult(new Dictionary<string, object?> { ["a"] = 1 }))
        {
            OnSecondMove = () => flushesBeforeSecondMove = writer.FlushCount
        };

        // act
        // the formatter runs until the stream waits for its second result
        var format = new JsonLinesResultFormatter(default)
            .FormatAsync(
                new ResponseStream(() => results, ExecutionResultKind.SubscriptionResult),
                writer,
                ExecutionResultFormatFlags.None,
                TestContext.Current.CancellationToken)
            .AsTask();
        results.Yield(CreateResult(new Dictionary<string, object?> { ["b"] = 2 }));
        await format;
        await writer.CompleteAsync();

        // assert
        Assert.Equal(1, flushesBeforeSecondMove);
        Assert.Equal(
            "{\"data\":{\"a\":1}}\n{\"data\":{\"b\":2}}\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }
}
