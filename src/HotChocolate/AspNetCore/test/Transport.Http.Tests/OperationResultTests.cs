using System.Text;
using System.Text.Json;

namespace HotChocolate.Transport.Http.Tests;

public class OperationResultTests
{
    [Fact]
    public void Parse_Should_ExposeIncrementalAndHasNext_When_PayloadIsIncremental()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes(
            """{"incremental":[{"data":{"name":"Abc"},"path":["product"]}],"hasNext":false}""");

        // act
        using var result = OperationResult.Parse(payload);

        // assert
        Assert.Equal(JsonValueKind.Array, result.Incremental.ValueKind);
        Assert.Equal(
            """[{"data":{"name":"Abc"},"path":["product"]}]""",
            result.Incremental.GetRawText());
        Assert.False(result.HasNext);
    }

    [Fact]
    public void Parse_Should_LeaveIncrementalAndHasNextUnset_When_PayloadIsNotIncremental()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes("""{"data":{"name":"Abc"}}""");

        // act
        using var result = OperationResult.Parse(payload);

        // assert
        Assert.Equal(JsonValueKind.Undefined, result.Incremental.ValueKind);
        Assert.Null(result.HasNext);
    }

    [Fact]
    public void Constructor_Should_KeepTheLegacySignature_When_IncrementalMembersWereAdded()
    {
        // arrange
        Type[] legacyParameters =
        [
            typeof(IDisposable),
            typeof(JsonElement),
            typeof(JsonElement),
            typeof(JsonElement),
            typeof(int?),
            typeof(int?)
        ];

        // act
        var constructor = typeof(OperationResult).GetConstructor(legacyParameters);

        // assert
        Assert.NotNull(constructor);
        Assert.All(constructor.GetParameters(), p => Assert.True(p.IsOptional));
    }

    [Fact]
    public void Constructor_Should_ExposeIncrementalAndHasNext_When_BothAreProvided()
    {
        // arrange
        using var document = JsonDocument.Parse("""[{"data":{"name":"Abc"},"path":["product"]}]""");

        // act
        using var result = new OperationResult(
            memoryOwner: null,
            data: default,
            errors: default,
            extensions: default,
            requestIndex: null,
            variableIndex: null,
            incremental: document.RootElement,
            hasNext: true);

        // assert
        Assert.Equal(JsonValueKind.Array, result.Incremental.ValueKind);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void Constructor_Should_LeaveIncrementalAndHasNextUnset_When_LegacyOverloadIsUsed()
    {
        // arrange
        // act
        using var result = new OperationResult();

        // assert
        Assert.Equal(JsonValueKind.Undefined, result.Incremental.ValueKind);
        Assert.Null(result.HasNext);
    }
}
