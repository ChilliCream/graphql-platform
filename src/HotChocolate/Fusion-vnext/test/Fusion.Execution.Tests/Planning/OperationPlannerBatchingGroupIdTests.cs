using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerBatchingGroupIdTests : FusionTestBase
{
    [Fact]
    public void Plan_With_RequestGrouping_Disabled_Assigns_No_BatchingGroupIds()
    {
        // arrange
        var schema = CreateBatchingSchema();

        // act
        var plan = PlanOperation(schema, QueryWithRepeatedLookups, enableRequestGrouping: false);

        // assert
        Assert.All(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => Assert.Null(node.BatchingGroupId));
    }

    [Fact]
    public void Plan_With_RequestGrouping_Enabled_Assigns_Deterministic_BatchingGroupIds()
    {
        // arrange
        var schema = CreateBatchingSchema();

        // act
        var plan1 = PlanOperation(schema, QueryWithRepeatedLookups, enableRequestGrouping: true);
        var plan2 = PlanOperation(schema, QueryWithRepeatedLookups, enableRequestGrouping: true);

        // assert
        // The two structurally equivalent schema-b lookups are merged into one
        // OperationBatchExecutionNode by the dedup optimization, but the BatchingGroupId
        // is retained from the pre-merge assignment.
        var schemaBBatchNode = Assert.Single(
            plan1.AllNodes.OfType<OperationBatchExecutionNode>(),
            t => t.SchemaName == "b");
        Assert.True(schemaBBatchNode.BatchingGroupId.HasValue);
        Assert.Equal(2, schemaBBatchNode.Targets.Length);

        // BatchingGroupIds must be deterministic across plan runs.
        var plan1Ids = plan1.AllNodes
            .OfType<OperationBatchExecutionNode>()
            .Select(t => t.BatchingGroupId)
            .OrderBy(id => id)
            .ToArray();
        var plan2Ids = plan2.AllNodes
            .OfType<OperationBatchExecutionNode>()
            .Select(t => t.BatchingGroupId)
            .OrderBy(id => id)
            .ToArray();
        Assert.Equal(plan1Ids, plan2Ids);
    }

    [Fact]
    public void CreateBatchingGroupLookup_Dependent_Query_Nodes_Do_Not_Share_Group()
    {
        // arrange
        var schema = CreateBatchingSchema();
        var queryType = schema.Types["Query"];
        var queryDefinition = ParseOperationDefinition(
            """
            query Step($id: ID!) {
              productById(id: $id) {
                rating
              }
            }
            """);

        var steps = ImmutableList.Create<PlanStep>(
            CreateStep(1, queryDefinition, queryType, "b", SelectionPath.Parse("$.a"), SelectionPath.Parse("$.productById")),
            CreateStep(2, queryDefinition, queryType, "b", SelectionPath.Parse("$.b"), SelectionPath.Parse("$.productById")),
            CreateStep(3, queryDefinition, queryType, "b", SelectionPath.Parse("$.c"), SelectionPath.Parse("$.productById")),
            CreateStep(10, queryDefinition, queryType, "b", SelectionPath.Parse("$.x"), SelectionPath.Parse("$.productById")),
            CreateStep(11, queryDefinition, queryType, "b", SelectionPath.Parse("$.y"), SelectionPath.Parse("$.productById")),
            CreateStep(12, queryDefinition, queryType, "b", SelectionPath.Parse("$.z"), SelectionPath.Parse("$.productById")));

        var dependencyLookup = new Dictionary<int, HashSet<int>>
        {
            [2] = [1],
            [3] = [2],
            [11] = [10],
            [12] = [11]
        };

        // act
        var lookup = OperationPlanner.CreateBatchingGroupLookup(
            steps,
            dependencyLookup,
            enableRequestGrouping: true);

        // assert
        Assert.Equal(lookup[1], lookup[10]);
        Assert.Equal(lookup[2], lookup[11]);
        Assert.Equal(lookup[3], lookup[12]);
        Assert.NotEqual(lookup[1], lookup[2]);
        Assert.NotEqual(lookup[1], lookup[3]);
        Assert.NotEqual(lookup[2], lookup[3]);
    }

    [Fact]
    public void CreateBatchingGroupLookup_Nodes_From_Different_Schemas_Do_Not_Share_Group()
    {
        // arrange
        var schema = CreateBatchingSchema();
        var queryType = schema.Types["Query"];
        var queryDefinition = ParseOperationDefinition(
            """
            query Step($id: ID!) {
              productById(id: $id) {
                rating
              }
            }
            """);

        var steps = ImmutableList.Create<PlanStep>(
            CreateStep(1, queryDefinition, queryType, "b", SelectionPath.Parse("$.a"), SelectionPath.Parse("$.productById")),
            CreateStep(2, queryDefinition, queryType, "b", SelectionPath.Parse("$.b"), SelectionPath.Parse("$.productById")),
            CreateStep(3, queryDefinition, queryType, "c", SelectionPath.Parse("$.c"), SelectionPath.Parse("$.productById")),
            CreateStep(4, queryDefinition, queryType, "c", SelectionPath.Parse("$.d"), SelectionPath.Parse("$.productById")));

        // act
        var lookup = OperationPlanner.CreateBatchingGroupLookup(
            steps,
            new Dictionary<int, HashSet<int>>(),
            enableRequestGrouping: true);

        // assert
        Assert.Equal(lookup[1], lookup[2]);
        Assert.Equal(lookup[3], lookup[4]);
        Assert.NotEqual(lookup[1], lookup[3]);
    }

    [Fact]
    public void Plan_NonQuery_Operation_Nodes_Do_Not_Get_BatchingGroupId()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            schema {
              query: Query
              mutation: Mutation
              subscription: Subscription
            }

            type Query {
              _empty: String
            }

            type Mutation {
              doWork: Int!
            }

            type Subscription {
              onWork: Int!
            }
            """);

        // act
        var mutationPlan = PlanOperation(
            schema,
            """
            mutation {
              doWork
            }
            """,
            enableRequestGrouping: true);
        var subscriptionPlan = PlanOperation(
            schema,
            """
            subscription {
              onWork
            }
            """,
            enableRequestGrouping: true);

        // assert
        var mutationNode = Assert.Single(mutationPlan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Equal(OperationType.Mutation, mutationNode.Operation.Type);
        Assert.Null(mutationNode.BatchingGroupId);

        var subscriptionNode = Assert.Single(subscriptionPlan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Equal(OperationType.Subscription, subscriptionNode.Operation.Type);
        Assert.Null(subscriptionNode.BatchingGroupId);
    }

    [Fact]
    public void Serialization_Includes_BatchingGroupId_When_Present()
    {
        // arrange
        var schema = CreateBatchingSchema();
        var plan = PlanOperation(schema, QueryWithoutRepeatedLookups, enableRequestGrouping: false);
        var initialJson = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }).Format(plan);
        var jsonDocument = JsonNode.Parse(initialJson)!;
        var operationNode = jsonDocument["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]!.GetValue<string>() is "Operation");
        operationNode["batchingGroupId"] = 42;

        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var parser = new JsonOperationPlanParser(new OperationCompiler(schema, pool));
        plan = parser.Parse(
            Encoding.UTF8.GetBytes(
                jsonDocument.ToJsonString(
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })));

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan);
        var json = new JsonOperationPlanFormatter().Format(plan);

        // assert
        Assert.Contains("batchingGroupId:", yaml, StringComparison.Ordinal);
        Assert.Contains("\"batchingGroupId\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("batchingGroupId: null", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("\"batchingGroupId\": null", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialization_Omits_BatchingGroupId_When_Null()
    {
        // arrange
        var schema = CreateBatchingSchema();
        var plan = PlanOperation(schema, QueryWithoutRepeatedLookups, enableRequestGrouping: false);

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan);
        var json = new JsonOperationPlanFormatter().Format(plan);

        // assert
        Assert.All(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => Assert.Null(node.BatchingGroupId));
        Assert.DoesNotContain("batchingGroupId:", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("\"batchingGroupId\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_Plan_Shows_BatchingGroup_When_Group_Is_Created()
    {
        // arrange
        var schema = CreateBatchingSchema();
        var plan = PlanOperation(schema, QueryWithRepeatedLookups, enableRequestGrouping: true);

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan);

        // assert
        Assert.Contains("batchingGroupId:", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("batchingGroupId: null", yaml, StringComparison.Ordinal);
        MatchSnapshot(plan);
    }

    private static OperationPlan PlanOperation(
        FusionSchemaDefinition schema,
        string operationText,
        bool enableRequestGrouping)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());

        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new DocumentRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(
            schema,
            compiler,
            new OperationPlannerOptions
            {
                EnableRequestGrouping = enableRequestGrouping
            });
        const string id = "123456789101112";
        return planner.CreatePlan(id, id, id, operation);
    }

    private static string CreateNodeSignature(OperationExecutionNode node)
    {
        return string.Concat(
            node.SchemaName ?? "__dynamic__",
            "|",
            node.Operation.Type.ToString(),
            "|",
            node.Operation.Hash,
            "|",
            node.Source.ToString(),
            "|",
            node.Target.ToString());
    }

    private static OperationDefinitionNode ParseOperationDefinition(
        string operationSourceText)
    {
        return Utf8GraphQLParser
            .Parse(operationSourceText)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
    }

    private static OperationPlanStep CreateStep(
        int id,
        OperationDefinitionNode definition,
        ITypeDefinition type,
        string? schemaName,
        SelectionPath target,
        SelectionPath source)
    {
        return new OperationPlanStep
        {
            Id = id,
            Definition = definition,
            Type = type,
            RootSelectionSetId = 1,
            SelectionSets = [1],
            SchemaName = schemaName,
            Target = target,
            Source = source
        };
    }

    private static FusionSchemaDefinition CreateBatchingSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              first: Product
              second: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              rating: Int!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              deliveryEstimate: Int!
            }
            """);
    }

    private const string QueryWithRepeatedLookups =
        """
        {
          first {
            id
            rating
            deliveryEstimate
          }
          second {
            id
            rating
            deliveryEstimate
          }
        }
        """;

    private const string QueryWithoutRepeatedLookups =
        """
        {
          first {
            id
            rating
            deliveryEstimate
          }
        }
        """;
}
