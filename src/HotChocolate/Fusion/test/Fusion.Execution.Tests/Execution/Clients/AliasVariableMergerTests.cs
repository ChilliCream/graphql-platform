using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class AliasVariableMergerTests
{
    [Fact]
    public void Write_Should_RenameEachRowsVariables_When_SingleOperationHasTwoRows()
    {
        // arrange
        // Mirrors the variable-batching -> alias-batching spec example variables object.
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

        // act
        var json = Merge(batched.Prefixes, requests);

        // assert
        json.MatchInlineSnapshot(
            """{"_0__fusion_2_id":"UHJvZHVjdDox","_1__fusion_2_id":"UHJvZHVjdDoy"}""");
    }

    [Fact]
    public void Write_Should_CarryOperationAndRowPrefixes_When_MultipleOperationsAreMerged()
    {
        // arrange
        // Mirrors the request-batching -> alias-batching spec example variables object.
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"UHJvZHVjdDox"}"""),
                Row("""{"__fusion_2_id":"UHJvZHVjdDoy"}""")),
            Request(
                """
                query Op($__fusion_3_id: ID!) {
                  reviewById(id: $__fusion_3_id) { body }
                }
                """,
                Row("""{"__fusion_3_id":"UHJvZHVjdDoy"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // act
        var json = Merge(batched.Prefixes, requests);

        // assert
        json.MatchInlineSnapshot(
            """{"_0_0__fusion_2_id":"UHJvZHVjdDox","_0_1__fusion_2_id":"UHJvZHVjdDoy","_1__fusion_3_id":"UHJvZHVjdDoy"}""");
    }

    [Fact]
    public void Write_Should_PreserveComplexValuesVerbatim_When_VariablesAreObjectsAndArrays()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_filter: FilterInput!, $__fusion_2_ids: [ID!]!) {
                  products(filter: $__fusion_1_filter, ids: $__fusion_2_ids) { name }
                }
                """,
                Row("""{"__fusion_1_filter":{"name":"a\"b","nested":{"x":1}},"__fusion_2_ids":["1","2"]}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // act
        var json = Merge(batched.Prefixes, requests);

        // assert
        // The embedded quote round-trips as the " escape the JSON encoder emits.
        json.MatchInlineSnapshot(
            """{"_0__fusion_1_filter":{"name":"a\u0022b","nested":{"x":1}},"_0__fusion_2_ids":["1","2"]}""");
    }

    [Fact]
    public void Write_Should_Throw_When_DeclaredVariableIsMissingFromRow()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID!) {
                  productById(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_other":"1"}""")));
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // act
        void Act() => Merge(batched.Prefixes, requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("__fusion_1_id", exception.Message);
    }

    private static string Merge(
        AliasPrefixTable prefixes,
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        using var buffer = new PooledArrayWriter();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            AliasVariableMerger.Write(writer, prefixes, requests);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
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
