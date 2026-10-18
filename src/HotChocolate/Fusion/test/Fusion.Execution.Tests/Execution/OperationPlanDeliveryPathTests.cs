using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
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
        Assert.Equal("Could not resolve selection path segment 'missing'.", exception.Message);
    }

    [Fact]
    public void GetDeliveryPath_Should_RejectUnknownGroup_When_PlanHasNoIncrementalParts()
    {
        // arrange
        var plan = CreatePlan("Empty");

        // act
        var exception = Assert.Throws<KeyNotFoundException>(() => plan.GetDeliveryPath(0));

        // assert
        Assert.Equal("The delivery group with the ID '0' was not found.", exception.Message);
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
