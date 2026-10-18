using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanDeliveryPathTests : FusionTestBase
{
    [Theory]
    [InlineData("Root", "0:  (0)")]
    [InlineData("Alias", "0: renamed (1)")]
    [InlineData("Deep", "0: renamed.profile (2)")]
    [InlineData("List", "0: users (1)")]
    [InlineData("Nested", "0: user (1)", "1: user (1)")]
    [InlineData("Empty")]
    public void Create_Should_PreparePendingPaths_When_PlanIsCreated(
        string shape,
        params string[] expected)
    {
        // arrange
        var plan = CreatePlan(shape);

        // act
        var paths = Describe(plan);

        // assert
        paths.MatchInlineSnapshots(expected);
    }

    [Theory]
    [InlineData("Root", "0:  (0)")]
    [InlineData("Alias", "0: renamed (1)")]
    [InlineData("Deep", "0: renamed.profile (2)")]
    [InlineData("List", "0: users (1)")]
    [InlineData("Nested", "0: user (1)", "1: user (1)")]
    [InlineData("Empty")]
    public void Parse_Should_PreparePendingPaths_When_PlanIsDeserialized(
        string shape,
        params string[] expected)
    {
        // arrange
        var plan = CreatePlan(shape);
        var formatter = new JsonOperationPlanFormatter();
        using var buffer = new PooledArrayWriter();
        formatter.Format(buffer, plan);
        var compiler = new OperationCompiler(
            (FusionSchemaDefinition)plan.Operation.Schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsed = parser.Parse(buffer.WrittenMemory);

        // assert
        Describe(parsed).MatchInlineSnapshots(expected);
        formatter.Format(parsed).MatchInlineSnapshot(formatter.Format(plan));
    }

    [Theory]
    [InlineData("Alias")]
    [InlineData("Deep")]
    [InlineData("List")]
    [InlineData("Nested")]
    public void GetDeliveryPath_Should_ReusePendingPaths_When_PlanIsReused(string shape)
    {
        // arrange
        var plan = CreatePlan(shape);
        var clone = plan with { };

        // act
        var first = plan.GetDeliveryPath(0);
        var second = plan.GetDeliveryPath(0);
        var cloned = clone.GetDeliveryPath(0);

        // assert
        Assert.Same(first.PendingPath, second.PendingPath);
        Assert.Same(first.PendingPath, cloned.PendingPath);
    }

    [Fact]
    public void Equals_Should_IgnoreDerivedPaths_When_EqualPlansAreCreated()
    {
        // arrange
        var original = CreatePlan("Alias");
        var first = OperationPlan.Create(
            "equal", original.Operation, [], [], original.DeliveryGroups, original.IncrementalPlans, 0, 0);
        var second = OperationPlan.Create(
            "equal", original.Operation, [], [], original.DeliveryGroups, original.IncrementalPlans, 0, 0);
        var clone = first with { };
        var plans = new HashSet<OperationPlan> { first };

        // act
        var firstPath = first.GetDeliveryPath(0);
        var secondPath = second.GetDeliveryPath(0);
        plans.Add(second);
        plans.Add(clone);

        // assert
        Assert.Equal(first, second);
        Assert.Equal(first, clone);
        Assert.NotSame(firstPath.PendingPath, secondPath.PendingPath);
        Assert.Single(plans);
    }

    [Fact]
    public void Equals_Should_DistinguishPlans_When_PlanningStatisticsDiffer()
    {
        // arrange
        var operation = CreatePlan("Empty").Operation;
        var first = OperationPlan.Create("equal", operation, [], [], [], [], 0, 0);
        var second = OperationPlan.Create("equal", operation, [], [], [], [], 1, 0);
        var third = OperationPlan.Create("equal", operation, [], [], [], [], 0, 1);

        // act
        var plans = new HashSet<OperationPlan> { first, second, third };

        // assert
        Assert.Equal(3, plans.Count);
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void GetDeliveryPath_Should_ReusePendingPaths_When_ReadersAreConcurrent()
    {
        // arrange
        var plan = CreatePlan("Nested");
        var paths = new OperationPlan.DeliveryPath[64];

        // act
        Parallel.For(0, paths.Length, i => paths[i] = plan.GetDeliveryPath(0));

        // assert
        Assert.All(paths, path => Assert.Same(paths[0].PendingPath, path.PendingPath));
        Describe(plan).MatchInlineSnapshots(["0: user (1)", "1: user (1)"]);
    }

    [Fact]
    public void Create_Should_RejectInvalidPath_When_MetadataCannotBePrepared()
    {
        // arrange
        var original = CreatePlan("Alias");
        var incremental = original.IncrementalPlans[0];
        var group = incremental.DeliveryGroups[0] with
        {
            Path = SelectionPath.Root.AppendField("missing")
        };
        var invalidIncrementalPlan = new IncrementalPlan(
            incremental.Operation,
            incremental.RootNodes,
            incremental.AllNodes,
            [group],
            incremental.Requirements);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => OperationPlan.Create(
            original.Id,
            original.Operation,
            original.RootNodes,
            original.AllNodes,
            [group],
            [invalidIncrementalPlan],
            original.SearchSpace,
            original.ExpandedNodes));

        // assert
        exception.Message.MatchInlineSnapshot("Could not resolve selection path segment 'missing'.");
    }

    [Fact]
    public void GetDeliveryPath_Should_RejectUnknownGroup_When_PlanHasNoIncrementalParts()
    {
        // arrange
        var plan = CreatePlan("Empty");

        // act
        var exception = Assert.Throws<KeyNotFoundException>(() => plan.GetDeliveryPath(0));

        // assert
        exception.Message.MatchInlineSnapshot("The delivery group with the ID '0' was not found.");
    }

    private static string[] Describe(OperationPlan plan)
        => plan.IncrementalPlans
            .SelectMany(incremental => incremental.DeliveryGroups)
            .Select(group => group.Id)
            .Distinct()
            .Order()
            .Select(id =>
            {
                var path = plan.GetDeliveryPath(id);
                return $"{id}: {path.PendingPath} ({path.PendingFieldCount})";
            })
            .ToArray();

    private static OperationPlan CreatePlan(string shape)
        => PlanOperation(CreateSchema(), shape switch
        {
            "Root" => "{ a ... @defer { b } }",
            "Alias" => "{ renamed: user(id: \"1\") { name ... @defer { email } } }",
            "Deep" => "{ renamed: user(id: \"1\") { name profile { ... @defer { bio } } } }",
            "List" => "{ users { name ... @defer { email } } }",
            "Nested" => "{ user(id: \"1\") { name ... @defer(label: \"outer\") { email ... @defer(label: \"inner\") { address } } } }",
            _ => "{ a }"
        });

    private static FusionSchemaDefinition CreateSchema()
        => ComposeSchema(
            """
            # name: a
            type Query {
                a: String
                user(id: ID!): User @lookup
                users: [User!]!
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                profile: Profile!
            }

            type Profile {
                bio: String!
            }
            """,
            """
            # name: b
            type Query {
                b: String
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
            }
            """);
}
