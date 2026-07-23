using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class AliasResponseReaderTests
{
    [Fact]
    public void Split_Should_RestoreOriginalFieldNames_When_SingleOperationHasTwoRows()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"UHJvZHVjdDox"}"""),
                Row("""{"__fusion_2_id":"UHJvZHVjdDoy"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(
            arena,
            """{"data":{"_0":{"name":"Table"},"_1":{"name":"Chair"}}}""");

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Table", results[0].Result.Data.GetProperty("productById").GetProperty("name").GetString());
        Assert.Equal("Chair", results[1].Result.Data.GetProperty("productById").GetProperty("name").GetString());
    }

    [Fact]
    public void Split_Should_EmitResultsInOperationAndRowOrder_When_MultipleOperationsAreMerged()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")),
            Request(
                """
                query Op($__fusion_3_id: ID!) {
                  reviewById(id: $__fusion_3_id) { body }
                }
                """,
                Row("""{"__fusion_3_id":"R1"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(
            arena,
            """
            {"data":{
              "_0_0":{"name":"Table"},
              "_0_1":{"name":"Chair"},
              "_1":{"body":"Great"}
            }}
            """);

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        Assert.Equal([0, 0, 1], results.Select(r => r.RequestIndex).ToArray());
        Assert.Equal("Table", results[0].Result.Data.GetProperty("productById").GetProperty("name").GetString());
        Assert.Equal("Great", results[2].Result.Data.GetProperty("reviewById").GetProperty("body").GetString());
    }

    [Fact]
    public void Split_Should_DecodeMultipleRoots_When_OperationHasSeveralRootFields()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Multi($__fusion_1_id: ID!, $__fusion_2_id: ID!) {
                  productById(id: $__fusion_1_id) { name }
                  reviewById(id: $__fusion_2_id) { body }
                }
                """,
                Row("""{"__fusion_1_id":"P1","__fusion_2_id":"R1"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(
            arena,
            """
            {"data":{
              "_0_0_productById":{"name":"Table"},
              "_0_0_reviewById":{"body":"Great"}
            }}
            """);

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        var data = Assert.Single(results).Result.Data;
        Assert.Equal("Table", data.GetProperty("productById").GetProperty("name").GetString());
        Assert.Equal("Great", data.GetProperty("reviewById").GetProperty("body").GetString());
    }

    [Fact]
    public void Split_Should_KeepPerRowDataReadable_When_MergedDocumentIsDisposed()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(arena, """{"data":{"_0":{"name":"Table"}}}""");

        // act
        // Split disposes the merged document before returning; the per-row document must survive.
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        Assert.Throws<ObjectDisposedException>(() => merged.Root.GetProperty("data"u8));
        var name = results[0].Result.Data.GetProperty("productById").GetProperty("name").GetString();
        Assert.Equal("Table", name);
    }

    [Fact]
    public void Split_Should_RouteErrorAndReplaceAliasWithResponseName_When_ErrorPathStartsWithAlias()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(
            arena,
            """
            {
              "data":{"_0":{"name":"Table"},"_1":null},
              "errors":[{"message":"boom","path":["_1","name"]}]
            }
            """);

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        // Only the second row carries the error, with its alias rewritten to productById.
        Assert.Null(results[0].Result.Errors);
        var error = results[1].Result.Errors!.Trie.FindFirstError();
        Assert.Equal("productById.name", error!.Path!.Print());
    }

    [Fact]
    public void Split_Should_BroadcastError_When_ErrorHasNoPath()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(
            arena,
            """
            {
              "data":{"_0":{"name":"Table"},"_1":{"name":"Chair"}},
              "errors":[{"message":"global failure"}]
            }
            """);

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        Assert.Equal(2, results.Count);
        Assert.Equal("global failure", SingleRootError(results[0]).Message);
        Assert.Equal("global failure", SingleRootError(results[1]).Message);
    }

    [Fact]
    public void Split_Should_SkipMissingSlots_When_AliasIsAbsentFromData()
    {
        // arrange
        using var arena = new MemoryArena();
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);
        var merged = Parse(arena, """{"data":{"_0":{"name":"Table"}}}""");

        // act
        var results = AliasResponseReader.Split(merged, arena, requests, batched);

        // assert
        // The second row is absent from the response, so only one result is emitted.
        var result = Assert.Single(results);
        Assert.Equal("Table", result.Result.Data.GetProperty("productById").GetProperty("name").GetString());
    }

    private static IError SingleRootError(AliasRowResult result)
        => Assert.Single(result.Result.Errors!.RootErrors);

    private static SourceResultDocument Parse(IMemoryArena arena, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return SourceResultDocument.Parse(arena, bytes, bytes.Length);
    }

    private static SourceSchemaClientRequest Request(string operation, params JsonSegment[] rows)
    {
        var variables = ImmutableArray.CreateBuilder<VariableValues>(rows.Length);

        foreach (var row in rows)
        {
            variables.Add(new VariableValues(CompactPath.Root, row));
        }

        return new SourceSchemaClientRequest
        {
            Node = null!,
            SchemaName = "test",
            OperationType = OperationType.Query,
            OperationSourceText = operation,
            OperationHash = operation.ComputeHash(),
            Variables = variables.ToImmutable()
        };
    }

    private static JsonSegment Row(string json)
    {
        var writer = new ChunkedArrayWriter();
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes);
        return JsonSegment.Create(writer, start, bytes.Length);
    }
}
