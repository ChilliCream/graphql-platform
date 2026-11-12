using System.IO.Pipelines;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Formatters;

public sealed class EventStreamResultFormatterTests
{
    [Fact]
    public async Task FormatAsync_Should_WriteNextEventsAndComplete_When_StreamEndsWithAnErrorOnlyResult()
    {
        // arrange
        var formatter = new EventStreamResultFormatter(default);
        var stream = new ResponseStream(ReadEventThenFaultAsync);
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));

        // act
        await formatter.FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        output.Position = 0;
        var content = await new StreamReader(output).ReadToEndAsync(TestContext.Current.CancellationToken);
        content.MatchInlineSnapshot(
            """
            event: next
            data: {"extensions":{"event":1}}

            event: next
            data: {"errors":[{"message":"Unexpected Execution Error"}]}

            event: complete
            data:


            """);
    }

    private static async IAsyncEnumerable<OperationResult> ReadEventThenFaultAsync()
    {
        yield return new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("event", 1));

        await Task.CompletedTask;

        yield return OperationResult.FromError(
            ErrorBuilder.New().SetMessage("Unexpected Execution Error").Build());
    }
}
