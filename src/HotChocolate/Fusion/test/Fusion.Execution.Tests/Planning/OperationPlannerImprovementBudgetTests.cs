using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerImprovementBudgetTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_ReturnGreedyPlan_When_ImprovementBudgetIsSpent()
    {
        // arrange
        var schema = CreateGreedyCoverSchema();
        var options = new OperationPlannerOptions
        {
            OperationWeight = 10.0,
            MaxPlanImprovementNodes = 1
        };

        // act
        var plan = PlanOperation(schema, GreedyCoverOperation, options);

        // assert
        GetSchemaNames(plan).MatchInlineSnapshots(["a", "b", "c"]);
    }

    [Fact]
    public void CreatePlan_Should_ReturnCheaperPlan_When_ImprovementBudgetIsDisabled()
    {
        // arrange
        var schema = CreateGreedyCoverSchema();
        var options = new OperationPlannerOptions
        {
            OperationWeight = 10.0,
            MaxPlanImprovementNodes = null
        };

        // act
        var plan = PlanOperation(schema, GreedyCoverOperation, options);

        // assert
        GetSchemaNames(plan).MatchInlineSnapshots(["b", "c"]);
    }

    [Fact]
    public void CreatePlan_Should_ReturnCheaperPlan_When_ImprovementBudgetIsDefault()
    {
        // arrange
        var schema = CreateGreedyCoverSchema();
        var options = new OperationPlannerOptions { OperationWeight = 10.0 };

        // act
        var plan = PlanOperation(schema, GreedyCoverOperation, options);

        // assert
        GetSchemaNames(plan).MatchInlineSnapshots(["b", "c"]);
    }

    [Fact]
    public void CreatePlan_Should_ExpandOnlyBudgetedNodes_When_GreedyPlanExists()
    {
        // arrange
        var schema = CreateGreedyCoverSchema();
        var options = new OperationPlannerOptions
        {
            OperationWeight = 10.0,
            MaxPlanImprovementNodes = 2
        };

        // act
        var plan = PlanOperation(schema, GreedyCoverOperation, options);

        // assert
        Assert.Equal(2, plan.ExpandedNodes);
    }

    [Fact]
    public void CreatePlan_Should_NotApplyBudget_When_NoCompletePlanExistsYet()
    {
        // arrange
        // the greedy pass finds no complete plan, so the search runs until it finds one
        var schema = CreateCircularCrossProviderMirrorSchema();
        var options = new OperationPlannerOptions { MaxPlanImprovementNodes = 1 };

        // act
        var plan = PlanOperation(schema, "{ feed { byNovice } }", options);

        // assert
        plan.AllNodes
            .OfType<OperationExecutionNode>()
            .Select(node => $"{node.Id}: {node.SchemaName}")
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  "1: a",
                  "3: b",
                  "4: a",
                  "5: b"
                ]
                """);
        Assert.True(plan.ExpandedNodes > 1);
    }

    [Fact]
    public void MaxPlanImprovementNodes_Should_DefaultToBoundedBudget()
    {
        // arrange
        var options = new OperationPlannerOptions();

        // act
        var value = options.MaxPlanImprovementNodes;

        // assert
        Assert.Equal(4096, value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxPlanImprovementNodes_Should_Throw_When_ValueIsBelowOne(int value)
    {
        // arrange
        var options = new OperationPlannerOptions();

        // act
        var error = Assert.Throws<ArgumentException>(
            () => options.MaxPlanImprovementNodes = value);

        // assert
        Assert.Equal("The planner max plan improvement nodes must be at least 1.", error.Message);
    }

    [Fact]
    public void MaxPlanImprovementNodes_Should_Throw_When_OptionsAreReadOnly()
    {
        // arrange
        var options = OperationPlannerOptions.Default;

        // act
        var error = Assert.Throws<InvalidOperationException>(
            () => options.MaxPlanImprovementNodes = 1);

        // assert
        Assert.Equal("The options are read-only.", error.Message);
    }

    private static string?[] GetSchemaNames(OperationPlan plan)
        => [.. plan.AllNodes
            .OfType<OperationExecutionNode>()
            .Select(node => node.SchemaName)
            .Order(StringComparer.Ordinal)];

    private const string GreedyCoverOperation = "{ f1 f2 f3 f4 f5 f6 }";

    private static FusionSchemaDefinition CreateGreedyCoverSchema()
        => ComposeSchema(
            """
            # name: a
            type Query {
                f1: Int @shareable
                f2: Int @shareable
                f3: Int @shareable
                f4: Int @shareable
            }
            """,
            """
            # name: b
            type Query {
                f1: Int @shareable
                f2: Int @shareable
                f5: Int
            }
            """,
            """
            # name: c
            type Query {
                f3: Int @shareable
                f4: Int @shareable
                f6: Int
            }
            """);

    private static FusionSchemaDefinition CreateCircularCrossProviderMirrorSchema()
        => ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
              authorById(id: ID! @is(field: "id")): Author @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              byExpert(byNovice: Boolean! @require(field: "byNovice")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
              authorById(id: ID! @is(field: "id")): Author @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author: Author!
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              rank: Int
            }
            """);
}
