using System.IO.Pipelines;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using static HotChocolate.AspNetCore.Serialization.ResultTestHelper;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class MultiPartResultFormatterTests
{
    private const string Header = "Content-Type: application/json; charset=utf-8\r\n\r\n";

    [Fact]
    public async Task FormatAsync_Should_WriteStreamBetweenResults_When_BatchContainsStream()
    {
        // arrange
        await using var output = new MemoryStream();
        var writer = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        var batch = new OperationResultBatch(
        [
            CreateResult(new Dictionary<string, object?> { ["a"] = 1 }),
            CreateStream(
                CreateResult(new Dictionary<string, object?> { ["b"] = 2 }),
                CreateResult(new Dictionary<string, object?> { ["c"] = 3 })),
            CreateResult(new Dictionary<string, object?> { ["d"] = 4 })
        ]);

        // act
        await new MultiPartResultFormatter().FormatAsync(
            batch,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        const string expected =
            "\r\n---\r\n" + Header + """{"data":{"a":1}}""" + "\r\n-----\r\n"
            + "\r\n---\r\n" + Header + """{"data":{"b":2}}"""
            + "\r\n---\r\n" + Header + """{"data":{"c":3}}"""
            + "\r\n-----\r\n"
            + "\r\n---\r\n" + Header + """{"data":{"d":4}}""" + "\r\n-----\r\n";
        Assert.Equal(expected, Encoding.UTF8.GetString(output.ToArray()));
        Assert.Equal(5, writer.FlushCount);
    }
}
