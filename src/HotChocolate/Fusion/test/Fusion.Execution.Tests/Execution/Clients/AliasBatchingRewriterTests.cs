using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class AliasBatchingRewriterTests
{
    [Fact]
    public void Rewrite_Should_ProduceAliasedRootsAndRenamedVariables_When_SingleOperationHasTwoRows()
    {
        // arrange
        // Mirrors the variable-batching -> alias-batching spec example.
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op_3a4e4b2f_3($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) {
                    name
                    price
                    weight
                    dimension { length width height }
                  }
                }
                """,
                Row("""{"__fusion_2_id":"UHJvZHVjdDox"}"""),
                Row("""{"__fusion_2_id":"UHJvZHVjdDoy"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Op_3a4e4b2f_3($_0__fusion_2_id: ID!, $_1__fusion_2_id: ID!) {
              _0: productById(id: $_0__fusion_2_id) {
                name
                price
                weight
                dimension {
                  length
                  width
                  height
                }
              }
              _1: productById(id: $_1__fusion_2_id) {
                name
                price
                weight
                dimension {
                  length
                  width
                  height
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_CarryOperationAndRowIndexInAliases_When_MultipleOperationsAreMerged()
    {
        // arrange
        // Mirrors the request-batching -> alias-batching spec example: op 0 has two
        // rows, op 1 has a single row and uses the bare _{op} alias and prefix.
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op_3a4e4b2f_3($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) {
                    name
                    price
                    weight
                    dimension { length width height }
                  }
                }
                """,
                Row("""{"__fusion_2_id":"UHJvZHVjdDox"}"""),
                Row("""{"__fusion_2_id":"UHJvZHVjdDoy"}""")),
            Request(
                """
                query Op_other($__fusion_3_id: ID!) {
                  reviewById(id: $__fusion_3_id) {
                    body
                  }
                }
                """,
                Row("""{"__fusion_3_id":"UHJvZHVjdDoy"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Op_3a4e4b2f_3(
              $_0_0__fusion_2_id: ID!
              $_0_1__fusion_2_id: ID!
              $_1__fusion_3_id: ID!
            ) {
              _0_0: productById(id: $_0_0__fusion_2_id) {
                name
                price
                weight
                dimension {
                  length
                  width
                  height
                }
              }
              _0_1: productById(id: $_0_1__fusion_2_id) {
                name
                price
                weight
                dimension {
                  length
                  width
                  height
                }
              }
              _1: reviewById(id: $_1__fusion_3_id) {
                body
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SuffixResponseNamePerRootField_When_OperationHasMultipleRoots()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Multi($__fusion_1_id: ID!, $__fusion_2_id: ID!) {
                  productById(id: $__fusion_1_id) { name }
                  reviewById(id: $__fusion_2_id) { body }
                }
                """,
                Row("""{"__fusion_1_id":"UDox","__fusion_2_id":"Ujox"}"""),
                Row("""{"__fusion_1_id":"UDoy","__fusion_2_id":"Ujoy"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Multi(
              $_0__fusion_1_id: ID!
              $_0__fusion_2_id: ID!
              $_1__fusion_1_id: ID!
              $_1__fusion_2_id: ID!
            ) {
              _0_0_productById: productById(id: $_0__fusion_1_id) {
                name
              }
              _0_0_reviewById: reviewById(id: $_0__fusion_2_id) {
                body
              }
              _0_1_productById: productById(id: $_1__fusion_1_id) {
                name
              }
              _0_1_reviewById: reviewById(id: $_1__fusion_2_id) {
                body
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_RenameVariableReferencesAtAnyDepth_When_UsedInDirectivesAndNestedArgs()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Deep($__fusion_1_id: ID!, $__fusion_2_skip: Boolean!) {
                  productById(id: $__fusion_1_id) {
                    name @skip(if: $__fusion_2_skip)
                    reviews(filter: { authorId: $__fusion_1_id }) { body }
                  }
                }
                """,
                Row("""{"__fusion_1_id":"UDox","__fusion_2_skip":false}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Deep($_0__fusion_1_id: ID!, $_0__fusion_2_skip: Boolean!) {
              _0: productById(id: $_0__fusion_1_id) {
                name @skip(if: $_0__fusion_2_skip)
                reviews(filter: { authorId: $_0__fusion_1_id }) {
                  body
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_PreserveVariableDefinitionDirectives_When_RenamingVariables()
    {
        // arrange
        // Variable-definition directives are const, so they cannot reference variables. The
        // renamer threads through them: the variable name is prefixed while the const
        // directive is preserved verbatim.
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID! @meta(tag: "primary")) {
                  productById(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_1_id":"UDox"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Op($_0__fusion_1_id: ID! @meta(tag: "primary")) {
              _0: productById(id: $_0__fusion_1_id) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_PreserveOperationDirectives_When_SingleOperationIsBatched()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID!) @trace(level: "debug") {
                  productById(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_1_id":"1"}"""),
                Row("""{"__fusion_1_id":"2"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            query Op($_0__fusion_1_id: ID!, $_1__fusion_1_id: ID!) @trace(level: "debug") {
              _0: productById(id: $_0__fusion_1_id) {
                name
              }
              _1: productById(id: $_1__fusion_1_id) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_MultiOperationMergeHasOperationDirectives()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID!) @trace {
                  productById(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_1_id":"1"}""")),
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  reviewById(id: $__fusion_2_id) { body }
                }
                """,
                Row("""{"__fusion_2_id":"2"}""")));

        // act
        void Act() => new AliasBatchingRewriter().Rewrite(requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("operation-level directives", exception.Message);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_RootSelectionIsInlineFragment()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID!) {
                  ... on Query {
                    productById(id: $__fusion_1_id) { name }
                  }
                }
                """,
                Row("""{"__fusion_1_id":"1"}""")));

        // act
        void Act() => new AliasBatchingRewriter().Rewrite(requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("field selections at the operation root", exception.Message);
    }

    [Fact]
    public void Rewrite_Should_OrderVariableDefinitionsDeterministically_When_CrossProductIsBuilt()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_a: ID!, $__fusion_2_b: ID!) {
                  productById(a: $__fusion_1_a, b: $__fusion_2_b) { name }
                }
                """,
                Row("""{"__fusion_1_a":"1","__fusion_2_b":"2"}"""),
                Row("""{"__fusion_1_a":"3","__fusion_2_b":"4"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        // Rows ascending, definitions in original order within each row.
        Assert.Equal(
            ["_0__fusion_1_a", "_0__fusion_2_b", "_1__fusion_1_a", "_1__fusion_2_b"],
            batched.Prefixes.PrefixedVariableNames);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_VariableNameStartsWithUnderscoreDigit()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($_0bad: ID!) {
                  productById(id: $_0bad) { name }
                }
                """,
                Row("""{"_0bad":"1"}""")));

        // act
        void Act() => new AliasBatchingRewriter().Rewrite(requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("_0bad", exception.Message);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_MultiOperationMergeContainsMutation()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_id: ID!) {
                  productById(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_1_id":"1"}""")),
            Mutation(
                """
                mutation Op($__fusion_2_id: ID!) {
                  deleteProduct(id: $__fusion_2_id) { id }
                }
                """,
                Row("""{"__fusion_2_id":"2"}""")));

        // act
        void Act() => new AliasBatchingRewriter().Rewrite(requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("mutation", exception.Message);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_RequestIsSubscription()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Subscription(
                """
                subscription Op($__fusion_1_id: ID!) {
                  onProductChanged(id: $__fusion_1_id) { name }
                }
                """,
                Row("""{"__fusion_1_id":"1"}""")));

        // act
        void Act() => new AliasBatchingRewriter().Rewrite(requests);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("subscription", exception.Message);
    }

    [Fact]
    public void Rewrite_Should_AllowSingleOperationMutation_When_BatchedAcrossRows()
    {
        // arrange
        var requests = ImmutableArray.Create(
            Mutation(
                """
                mutation Op($__fusion_1_id: ID!) {
                  deleteProduct(id: $__fusion_1_id) { id }
                }
                """,
                Row("""{"__fusion_1_id":"1"}"""),
                Row("""{"__fusion_1_id":"2"}""")));

        // act
        var batched = new AliasBatchingRewriter().Rewrite(requests);

        // assert
        batched.SourceText.MatchInlineSnapshot(
            """
            mutation Op($_0__fusion_1_id: ID!, $_1__fusion_1_id: ID!) {
              _0: deleteProduct(id: $_0__fusion_1_id) {
                id
              }
              _1: deleteProduct(id: $_1__fusion_1_id) {
                id
              }
            }
            """);
    }

    private static SourceSchemaClientRequest Request(string operation, params JsonSegment[] rows)
        => CreateRequest(operation, OperationType.Query, rows);

    private static SourceSchemaClientRequest Mutation(string operation, params JsonSegment[] rows)
        => CreateRequest(operation, OperationType.Mutation, rows);

    private static SourceSchemaClientRequest Subscription(string operation, params JsonSegment[] rows)
        => CreateRequest(operation, OperationType.Subscription, rows);

    private static SourceSchemaClientRequest CreateRequest(
        string operation,
        OperationType operationType,
        JsonSegment[] rows)
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
            OperationType = operationType,
            OperationSourceText = operation,
            OperationHash = operation.ComputeHash(),
            Variables = variables.ToImmutable()
        };
    }

    private static JsonSegment Row(string json)
    {
        // The rewriter only reads the variable row count, but a realistic JSON
        // segment keeps the test fixtures aligned with the merger's expectations.
        var writer = new ChunkedArrayWriter();
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes);
        return JsonSegment.Create(writer, start, bytes.Length);
    }
}
