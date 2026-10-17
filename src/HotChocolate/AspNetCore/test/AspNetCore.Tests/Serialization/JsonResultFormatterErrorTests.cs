using System.Text;
using System.Text.Encodings.Web;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using static HotChocolate.AspNetCore.Serialization.ResultTestHelper;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class JsonResultFormatterErrorTests
{
    [Fact]
    public async Task Format_Should_PreserveMetadataAndEncoder_When_ResultHasSingleError()
    {
        // arrange
        var error = ErrorBuilder.New()
            .SetMessage("<failure>")
            .SetPath(Path.Root.Append("products").Append(1).Append("name"))
            .AddLocation(new Location(3, 8))
            .AddLocation(new Location(1, 2))
            .SetCode("FAILED")
            .SetExtension("details", new RawJsonValue("{\"retry\":false}"u8.ToArray()))
            .Build();
        await using var result = OperationResult.FromError(error);
        using var buffer = new PooledArrayWriter();
        var formatter = new JsonResultFormatter(new JsonResultFormatterOptions { Encoder = JavaScriptEncoder.Default });

        // act
        formatter.Format(result, buffer);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
    }

    [Fact]
    public async Task Format_Should_KeepStablePathOrder_When_ResultHasMultipleErrors()
    {
        // arrange
        await using var result = new OperationResult(
        [
            ErrorBuilder.New().SetMessage("second").SetPath(Path.Root.Append("b")).Build(),
            ErrorBuilder.New().SetMessage("first duplicate").SetPath(Path.Root.Append("a")).Build(),
            ErrorBuilder.New().SetMessage("root").Build(),
            ErrorBuilder.New().SetMessage("last duplicate").SetPath(Path.Root.Append("a")).Build()
        ]);
        using var buffer = new PooledArrayWriter();

        // act
        JsonResultFormatter.Default.Format(result, buffer);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
    }

    [Fact]
    public async Task Format_Should_KeepData_When_ResultHasNoErrors()
    {
        // arrange
        await using var result = CreateResult(new Dictionary<string, object?> { ["value"] = 1 });
        using var buffer = new PooledArrayWriter();

        // act
        JsonResultFormatter.Default.Format(result, buffer);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchInlineSnapshot("""{"data":{"value":1}}""");
    }
}
