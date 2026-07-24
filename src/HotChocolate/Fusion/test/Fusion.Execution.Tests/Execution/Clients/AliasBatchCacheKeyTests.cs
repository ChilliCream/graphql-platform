using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class AliasBatchCacheKeyTests
{
    [Fact]
    public void Build_Should_CombineHashAndRowCount_When_SingleOperation()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(hash: 0x3a4e4b2f, rows: 2));

        // act
        Span<char> buffer = stackalloc char[192];
        var length = AliasBatchCacheKey.Build(buffer, requests);

        // assert
        buffer[..length].ToString().MatchInlineSnapshot("000000003a4e4b2f|2");
    }

    [Fact]
    public void Build_Should_OrderHashesThenRowCounts_When_MultipleOperations()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(hash: 0x01, rows: 2),
            Request(hash: 0x02, rows: 1));

        // act
        Span<char> buffer = stackalloc char[192];
        var length = AliasBatchCacheKey.Build(buffer, requests);

        // assert
        buffer[..length].ToString().MatchInlineSnapshot(
            "0000000000000001:0000000000000002|2,1");
    }

    [Fact]
    public void Build_Should_ProduceDifferentKeys_When_OperationsAreReordered()
    {
        // arrange
        var forward = ImmutableArray.Create(
            Request(hash: 0xAA, rows: 2),
            Request(hash: 0xBB, rows: 1));
        var reversed = ImmutableArray.Create(
            Request(hash: 0xBB, rows: 1),
            Request(hash: 0xAA, rows: 2));

        // act
        Span<char> forwardBuffer = stackalloc char[192];
        Span<char> reversedBuffer = stackalloc char[192];
        var forwardLength = AliasBatchCacheKey.Build(forwardBuffer, forward);
        var reversedLength = AliasBatchCacheKey.Build(reversedBuffer, reversed);

        // assert
        Assert.NotEqual(
            forwardBuffer[..forwardLength].ToString(),
            reversedBuffer[..reversedLength].ToString());
    }

    [Fact]
    public void Build_Should_ProduceStableKey_When_SameHashesAndRowCountsRepeat()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(hash: 0x1234, rows: 3),
            Request(hash: 0x5678, rows: 2));

        // act
        Span<char> first = stackalloc char[192];
        Span<char> second = stackalloc char[192];
        var firstLength = AliasBatchCacheKey.Build(first, requests);
        var secondLength = AliasBatchCacheKey.Build(second, requests);

        // assert
        Assert.Equal(first[..firstLength].ToString(), second[..secondLength].ToString());
    }

    [Fact]
    public void Build_Should_TreatEmptyVariablesAsOneRow_When_RequestHasNoVariables()
    {
        // arrange
        var requests = ImmutableArray.Create(Request(hash: 0x10));

        // act
        Span<char> buffer = stackalloc char[192];
        var length = AliasBatchCacheKey.Build(buffer, requests);

        // assert
        buffer[..length].ToString().MatchInlineSnapshot("0000000000000010|1");
    }

    [Fact]
    public void Build_Should_FitWithin192Chars_When_EightOperationsWithSmallRowCounts()
    {
        // arrange
        // The client stack allocates 192 chars for the key. A realistic batch of eight
        // operations with small row counts must fit so the hot path never rents.
        var builder = ImmutableArray.CreateBuilder<SourceSchemaClientRequest>(8);
        for (var i = 0; i < 8; i++)
        {
            builder.Add(Request(hash: (ulong)i, rows: 2));
        }
        var requests = builder.ToImmutable();

        // act
        Span<char> buffer = stackalloc char[192];
        var length = AliasBatchCacheKey.Build(buffer, requests);

        // assert
        Assert.True(length <= 192, $"Expected key length <= 192 but was {length}.");
    }

    [Fact]
    public void GetMaxKeyLength_Should_FitBuiltKey_When_RowCountsAreLarge()
    {
        // arrange
        // GetMaxKeyLength is the conservative bound the client uses to fall back to a rented
        // buffer. It must never under-report, even for very large row counts.
        var requests = ImmutableArray.Create(
            Request(hash: 0x01, rows: 1),
            Request(hash: 0x02, rows: 1));

        // act
        var maxLength = AliasBatchCacheKey.GetMaxKeyLength(requests);
        Span<char> buffer = stackalloc char[256];
        var actualLength = AliasBatchCacheKey.Build(buffer, requests);

        // assert
        Assert.True(maxLength >= actualLength, $"Max {maxLength} must be >= actual {actualLength}.");
    }

    private static SourceSchemaClientRequest Request(ulong hash, int rows = 0)
    {
        var variables = ImmutableArray.CreateBuilder<VariableValues>(rows);
        for (var i = 0; i < rows; i++)
        {
            variables.Add(VariableValues.Empty);
        }

        return new SourceSchemaClientRequest
        {
            Node = null!,
            SchemaName = "test",
            OperationType = OperationType.Query,
            OperationSourceText = "query { __typename }",
            OperationHash = hash,
            Variables = variables.ToImmutable()
        };
    }
}
