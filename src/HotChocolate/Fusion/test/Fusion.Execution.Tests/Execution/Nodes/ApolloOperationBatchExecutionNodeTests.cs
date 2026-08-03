using System.Text;
using HotChocolate.Fusion.Types;

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
    public void CreateFromLookup_Should_BuildPerLookupDocuments_When_TwoLookupDefinitions()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);

        // act
        var node = ApolloOperationBatchExecutionNode.CreateFromLookup(1, operations, schema);

        // assert
        // Each lookup keeps its own rewritten '_entities' document declaring
        // its own representations variable.
        Assert.Equal("b", node.SchemaName);
        Assert.All(
            node.Lookups.ToArray(),
            lookup => Assert.False(lookup.RepresentationShape.IsDefault));
        string[] documents =
        [
            Encoding.UTF8.GetString(node.Lookups[0].Operation.Value.Span),
            Encoding.UTF8.GetString(node.Lookups[1].Operation.Value.Span)
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
    public void CreateFromParser_Should_MaterializeEmptyRepresentationShape_When_NoRequirements()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);
        var plannedBatch = ApolloOperationBatchExecutionNode.CreateFromLookup(1, operations, schema);
        var operation = operations[0];
        var plannedLookup = plannedBatch.Lookups[0];

        // act
        var node = ApolloOperationExecutionNode.CreateFromParser(
            1,
            plannedLookup.Operation,
            plannedLookup.EntityTypeName,
            operation.SchemaName!,
            operation.Target,
            requirements: [],
            forwardedVariables: [.. operation.ForwardedVariables],
            operation.ResultSelectionSet,
            conditions: [.. operation.Conditions],
            operation.RequiresFileUpload,
            schema);

        // assert
        Assert.False(node.Lookup.RepresentationShape.IsDefault);
        Assert.Empty(node.Lookup.RepresentationShape);
        Assert.NotNull(node.Lookup.OperationDocument);
    }

    [Fact]
    public void CreateFromParser_Should_MaterializeEmptyRepresentationShapes_When_BatchHasNoRequirements()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var plannedOperations = GetLookupDefinitions(plan);
        var plannedBatch = ApolloOperationBatchExecutionNode.CreateFromLookup(1, plannedOperations, schema);
        var operations = new SingleOperationDefinition[plannedOperations.Length];
        var lookups = new ApolloEntityLookup[plannedOperations.Length];

        for (var i = 0; i < plannedOperations.Length; i++)
        {
            operations[i] = WithoutRequirements(plannedOperations[i]);
            lookups[i] = new ApolloEntityLookup(
                plannedBatch.Lookups[i].Operation,
                plannedBatch.Lookups[i].OperationDocument,
                plannedBatch.Lookups[i].EntityTypeName,
                RepresentationShape: default);
        }

        // act
        var node = ApolloOperationBatchExecutionNode.CreateFromParser(
            1,
            operations,
            lookups,
            schema);

        // assert
        Assert.All(
            node.Lookups.ToArray(),
            lookup =>
            {
                Assert.False(lookup.RepresentationShape.IsDefault);
                Assert.Empty(lookup.RepresentationShape);
                Assert.NotNull(lookup.OperationDocument);
            });
    }

    [Fact]
    public void CreateFromLookup_Should_Throw_When_SingleDefinition()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);

        // act
        void Act() => ApolloOperationBatchExecutionNode.CreateFromLookup(1, [operations[0]], schema);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith(
            "An Apollo entity batch requires at least two operation definitions.",
            exception.Message);
    }

    [Fact]
    public void CreateFromParser_Should_Throw_When_LookupCountDiffers()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var operations = GetLookupDefinitions(plan);

        // act
        void Act() => ApolloOperationBatchExecutionNode.CreateFromParser(
            1,
            operations,
            [],
            schema);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith(
            "An Apollo entity batch requires one parsed entity lookup per operation definition.",
            exception.Message);
    }

    [Fact]
    public void CreateFromLookup_Should_Throw_When_SchemaNamesDiffer()
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
        void Act() => ApolloOperationBatchExecutionNode.CreateFromLookup(
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
                    lookupNode.LookupTypeName,
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

    private static SingleOperationDefinition WithoutRequirements(
        SingleOperationDefinition operation)
        => new(
            operation.Id,
            operation.SourceText,
            operation.LookupTypeName,
            operation.SchemaName,
            operation.Target,
            operation.Source,
            requirements: [],
            forwardedVariables: [.. operation.ForwardedVariables],
            operation.ResultSelectionSet,
            conditions: [.. operation.Conditions],
            operation.RequiresFileUpload);
}
