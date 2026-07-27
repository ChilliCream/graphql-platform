namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ApolloOperationBatchExecutionNodeTests : FusionTestBase
{
    private const string SchemaA =
        """
        # name: a
        type Query {
          foos: [Foo]
          bars: [Bar]
        }

        type Foo @key(fields: "id") {
          id: ID!
        }

        type Bar @key(fields: "id") {
          id: ID!
        }
        """;

    private const string SchemaB =
        """
        # name: b
        type Query {
          fooById(id: ID! @is(field: "id")): Foo @lookup @internal
          barById(id: ID! @is(field: "id")): Bar @lookup @internal
        }

        type Foo @key(fields: "id") {
          id: ID!
          name: String
        }

        type Bar @key(fields: "id") {
          id: ID!
          title: String
        }
        """;

    [Fact]
    public void Create_Should_BuildPerLookupDocuments_When_TwoLookupDefinitions()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);

        // act
        var node = ApolloOperationBatchExecutionNode.Create(1, operations, schema);

        // assert
        // Each lookup keeps its own rewritten '_entities' document declaring
        // its own representations variable.
        Assert.Equal("b", node.SchemaName);
        string[] documents =
        [
            node.Lookups[0].Operation.SourceText,
            node.Lookups[1].Operation.SourceText
        ];
        documents.MatchInlineSnapshots(
        [
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Bar {
                  title
                }
              }
            }
            """,
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Foo {
                  name
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_SingleDefinition()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);

        // act
        void Act() => ApolloOperationBatchExecutionNode.Create(1, [operations[0]], schema);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith(
            "An Apollo entity batch requires at least two operation definitions.",
            exception.Message);
    }

    [Fact]
    public void Create_Should_Throw_When_SchemaNamesDiffer()
    {
        // arrange
        const string schemaC =
            """
            # name: c
            type Query {
              barById(id: ID! @is(field: "id")): Bar @lookup @internal
            }

            type Bar @key(fields: "id") {
              id: ID!
              title: String
            }
            """;
        const string schemaAWithoutBar =
            """
            # name: a
            type Query {
              bars: [Bar]
            }

            type Bar @key(fields: "id") {
              id: ID!
            }
            """;
        var schema = ComposeSchema(SchemaA, SchemaB);
        var otherSchema = ComposeSchema(schemaAWithoutBar, schemaC);
        var operations = GetLookupDefinitions(plan: PlanOperation(schema, "{ foos { id name } bars { id title } }"));
        var otherPlan = PlanOperation(otherSchema, "{ bars { id title } }");
        var otherOperation = GetSingleLookupDefinition(otherPlan);

        // act
        void Act() => ApolloOperationBatchExecutionNode.Create(
            1,
            [operations[0], otherOperation],
            schema);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith(
            "All operation definitions of an Apollo entity batch must target the same source schema.",
            exception.Message);
    }

    private static SingleOperationDefinition[] GetLookupDefinitions(OperationPlan plan)
    {
        foreach (var node in plan.AllNodes)
        {
            if (node is OperationBatchExecutionNode batchNode)
            {
                var definitions = new SingleOperationDefinition[batchNode.Operations.Length];

                for (var i = 0; i < batchNode.Operations.Length; i++)
                {
                    definitions[i] = Assert.IsType<SingleOperationDefinition>(batchNode.Operations[i]);
                }

                return definitions;
            }
        }

        throw new InvalidOperationException("The plan does not contain an operation batch node.");
    }

    private static SingleOperationDefinition GetSingleLookupDefinition(OperationPlan plan)
    {
        foreach (var node in plan.AllNodes)
        {
            if (node is OperationExecutionNode { Dependencies.Length: > 0 } lookupNode)
            {
                return new SingleOperationDefinition(
                    lookupNode.Id,
                    lookupNode.Operation,
                    lookupNode.SchemaName,
                    lookupNode.Target,
                    lookupNode.Source,
                    [.. lookupNode.Requirements],
                    [.. lookupNode.ForwardedVariables],
                    lookupNode.ResultSelectionSet,
                    [.. lookupNode.Conditions],
                    lookupNode.RequiresFileUpload);
            }
        }

        throw new InvalidOperationException("The plan does not contain a lookup definition.");
    }
}
