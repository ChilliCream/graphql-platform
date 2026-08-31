using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

public class IncrementalResultTests
{
    [Fact]
    public void IncrementalListResult_Should_ContainOneFormattedItem()
    {
        // arrange
        var value = new object();
        var formatter = new TestFormatter();
        var item = new OperationResultData(value, false, formatter, null);

        // act
        IIncrementalListResult result = new IncrementalListResult(123, item);

        // assert
        Assert.Equal(123, result.Id);
        Assert.Single(result.Items);
        Assert.Same(value, result.Items[0].Value);
        Assert.Same(formatter, result.Items[0].Formatter);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void IncrementalResults_Should_ExposeExtensions_When_Present()
    {
        // arrange
        IReadOnlyDictionary<string, object?> extensions = new Dictionary<string, object?>
        {
            ["example"] = "value"
        };
        var item = new OperationResultData(new object(), false, new TestFormatter(), null);

        // act
        var pending = new PendingResult(1, Path.Root, Extensions: extensions);
        var completed = new CompletedResult(1, Extensions: extensions);
        IIncrementalResult objectResult = new IncrementalObjectResult(1, extensions: extensions);
        IIncrementalResult listResult = new IncrementalListResult(1, item, extensions: extensions);

        // assert
        Assert.Same(extensions, pending.Extensions);
        Assert.Same(extensions, completed.Extensions);
        Assert.Same(extensions, objectResult.Extensions);
        Assert.Same(extensions, listResult.Extensions);
    }

    [Fact]
    public void IncrementalResults_Should_NotExposeExtensions_When_Absent()
    {
        // arrange
        var item = new OperationResultData(new object(), false, new TestFormatter(), null);

        // act
        var pending = new PendingResult(1, Path.Root);
        var completed = new CompletedResult(1);
        IIncrementalResult objectResult = new IncrementalObjectResult(1);
        IIncrementalResult listResult = new IncrementalListResult(1, item);

        // assert
        Assert.Null(pending.Extensions);
        Assert.Null(completed.Extensions);
        Assert.Null(objectResult.Extensions);
        Assert.Null(listResult.Extensions);
    }

    private sealed class TestFormatter : IRawJsonFormatter
    {
        public void WriteDataTo(JsonWriter jsonWriter)
        {
        }
    }
}
