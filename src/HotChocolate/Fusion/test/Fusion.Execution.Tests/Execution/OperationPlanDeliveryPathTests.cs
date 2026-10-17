using System.Collections;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanDeliveryPathTests : FusionTestBase
{
    private static readonly Func<OperationPlan, IDictionary> s_createDeliveryPaths =
        typeof(OperationPlanExecutor)
            .GetMethod("CreateDeliveryPaths", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<OperationPlan, IDictionary>>();

    [Theory]
    [InlineData("Root", "0:  (0)")]
    [InlineData("Alias", "0: renamed (1)")]
    [InlineData("Deep", "0: renamed.profile (2)")]
    [InlineData("List", "0: users (1)")]
    [InlineData("Nested", "0: user (1)", "1: user (1)")]
    [InlineData("Empty")]
    public void CreateDeliveryPaths_Should_PreservePendingPaths_When_PlanIsReused(
        string shape,
        params string[] expected)
    {
        // arrange
        var plan = CreatePlan(shape);

        // act
        var first = s_createDeliveryPaths(plan);
        var second = s_createDeliveryPaths(plan);

        // assert
        Assert.Equal(expected, Describe(first));
        Assert.Equal(expected, Describe(second));
        Assert.Same(first, second);
    }

    [Fact]
    public void CreateDeliveryPaths_Should_PreserveRecordEqualityAndHash_When_EqualPlansAreInitialized()
    {
        // arrange
        var operation = CreatePlan("Empty").Operation;
        var first = OperationPlan.Create("equal", operation, [], [], [], [], 0, 0);
        var second = OperationPlan.Create("equal", operation, [], [], [], [], 0, 0);
        var clone = first with { };
        var hash = first.GetHashCode();

        // act
        var firstPaths = s_createDeliveryPaths(first);
        var secondPaths = s_createDeliveryPaths(second);
        var clonePaths = s_createDeliveryPaths(clone);

        // assert
        Assert.Equal(first, second);
        Assert.Equal(first, clone);
        Assert.Equal(hash, first.GetHashCode());
        Assert.NotSame(firstPaths, secondPaths);
        Assert.NotSame(firstPaths, clonePaths);
    }

    [Fact]
    public async Task CreateDeliveryPaths_Should_PublishOneMap_When_FirstUseIsConcurrent()
    {
        // arrange
        var plan = CreatePlan("Nested");

        // act
        var maps = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => s_createDeliveryPaths(plan))));

        // assert
        Assert.Equal(["0: user (1)", "1: user (1)"], Describe(maps[0]));
        Assert.All(maps, map => Assert.Same(maps[0], map));
    }

    [Fact]
    public void CreateDeliveryPaths_Should_RetryFailedBuild_When_PathIsInvalid()
    {
        // arrange
        var original = CreatePlan("Alias");
        var incremental = original.IncrementalPlans[0];
        var group = incremental.DeliveryGroups[0] with
        {
            Path = SelectionPath.Root.AppendField("missing")
        };
        var invalid = OperationPlan.Create(
            original.Id,
            original.Operation,
            original.RootNodes,
            original.AllNodes,
            [group],
            [new IncrementalPlan(
                incremental.Operation,
                incremental.RootNodes,
                incremental.AllNodes,
                [group],
                incremental.Requirements)],
            original.SearchSpace,
            original.ExpandedNodes);

        // act
        var first = Assert.Throws<InvalidOperationException>(() => s_createDeliveryPaths(invalid));
        var second = Assert.Throws<InvalidOperationException>(() => s_createDeliveryPaths(invalid));

        // assert
        Assert.Equal("Could not resolve selection path segment 'missing'.", first.Message);
        Assert.Equal(first.Message, second.Message);
        Assert.NotSame(first, second);
    }

    private static string[] Describe(IDictionary paths)
        => paths.Keys.Cast<int>()
            .Order()
            .Select(key =>
            {
                var value = paths[key]!;
                var type = value.GetType();
                var path = (Path)type.GetProperty("PendingPath")!.GetValue(value)!;
                var count = (int)type.GetProperty("PendingFieldCount")!.GetValue(value)!;
                return $"{key}: {path} ({count})";
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
