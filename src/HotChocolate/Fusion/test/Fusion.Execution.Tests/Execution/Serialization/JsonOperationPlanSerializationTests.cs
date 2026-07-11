using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Serialization;

public class JsonOperationPlanSerializationTests : FusionTestBase
{
    [Fact]
    public void Parse_Plan()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                estimatedDelivery(postCode: "12345")
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    // Source schema A defines the Media interface and its implementing types; source schema B is
    // an @interfaceObject stand-in contributing "views". A value produced by B is opaque, and that
    // opacity is derived when the result selection set is (re)built, so it must survive a plan
    // serialize/parse round-trip for a cached plan to execute correctly.
    private const string InterfaceObjectSchemaA =
        """
        # name: a
        type Query {
          mediaById(id: ID!): Media @lookup
        }
        interface Media {
          id: ID!
          title: String!
        }
        type Book implements Media @key(fields: "id") {
          id: ID!
          title: String!
          isbn: String!
        }
        type Movie implements Media @key(fields: "id") {
          id: ID!
          title: String!
          runtime: Int!
        }
        """;

    private const string InterfaceObjectSchemaB =
        """
        # name: b
        type Query {
          trendingMedia: [Media!]!
          mediaByKey(id: ID!): Media @lookup @internal
        }
        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    [Fact]
    public void Parse_Plan_Preserves_InterfaceObject_Opacity_On_StandIn_Fetch()
    {
        // arrange
        var compositeSchema = ComposeSchema(InterfaceObjectSchemaA, InterfaceObjectSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query {
              trendingMedia {
                __typename
                id
                views
              }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var planSource = Encoding.UTF8.GetBytes(formatter.Format(originalPlan));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        // The stand-in fetch (schema b) resolves { id views }; its trendingMedia child must carry
        // the opacity marker after deserialization so the executor completes it interface-typed.
        var standInNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.SchemaName == "b");
        var opaqueChild = standInNode.ResultSelectionSet.TryGetChild("trendingMedia");
        Assert.NotNull(opaqueChild);
        Assert.True(opaqueChild.ProducesOpaqueElements);
    }

    [Fact]
    public void Parse_Plan_Preserves_LazySkipped_InterfaceObject_Plan()
    {
        // arrange
        var compositeSchema = ComposeSchema(InterfaceObjectSchemaA, InterfaceObjectSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query {
              trendingMedia {
                id
              }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var planSource = Encoding.UTF8.GetBytes(formatter.Format(originalPlan));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        // Neither __typename nor any type-conditioned field is selected, so the plan is a single
        // stand-in fetch with no covering lookup, and that shape survives the round-trip while the
        // value still completes interface-typed (opacity preserved).
        var standInNode = Assert.Single(parsedPlan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Equal("b", standInNode.SchemaName);
        var opaqueChild = standInNode.ResultSelectionSet.TryGetChild("trendingMedia");
        Assert.NotNull(opaqueChild);
        Assert.True(opaqueChild.ProducesOpaqueElements);
    }

    [Fact]
    public void Parse_Plan_Uses_SelectionSet_Syntax_When_Present()
    {
        // arrange
        // Inject a custom selection set string into the formatted plan, then
        // parse it back to confirm the parser preserves the syntax.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var json = JsonNode.Parse(formatter.Format(originalPlan))!;
        var operationNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Operation");
        var operationNodeId = operationNode["id"]!.GetValue<int>();
        operationNode["resultSelectionSet"] = "{ __typename }";
        var planSource = Encoding.UTF8.GetBytes(json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        var parsedOperationNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.Id == operationNodeId);
        Assert.Equal("{ __typename }", parsedOperationNode.ResultSelectionSet.ToString(indented: false));
    }

    [Fact]
    public void Parse_Plan_With_Node()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
              authorById(id: ID!): Author @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }

            type Author implements Node {
              id: ID!
              name: String!
            }
            """);

        var compositeSchema = ComposeSchema(source1);

        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
              a: node(id: "abc") {
                id
                ... on Discussion {
                  title
                }
                ... on Author {
                  name
                }
              }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var parsedPlanFormatted = formatter.Format(parsedPlan);
        parsedPlanFormatted.MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_With_Apollo_Lookup()
    {
        // arrange
        var compositeSchema = ComposeApolloSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        Assert.Single(originalPlan.AllNodes.OfType<ApolloOperationExecutionNode>());
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_With_Apollo_Lookup_Batch()
    {
        // arrange
        // Two entity lookups against the same Apollo source schema at the same
        // depth are grouped into a single Apollo batch node.
        var compositeSchema = ComposeApolloSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
                brands {
                    id
                    name
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        MatchSnapshot(originalPlan);
        Assert.Single(originalPlan.AllNodes.OfType<ApolloOperationBatchExecutionNode>());
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_Preserves_DeliveryGroup_Identity_Across_Plan_And_IncrementalPlans()
    {
        // arrange
        // Two sibling @defer fragments share a field (email) plus a nested @defer
        // adds a parent chain. Round-trip must restore canonical DeliveryGroup instances.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        ... @defer(label: "nested") {
                            address
                        }
                    }
                    ... @defer(label: "location") {
                        email
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
        Assert.All(
            parsedPlan.IncrementalPlans,
            p => Assert.All(
                p.DeliveryGroups,
                g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Id), g)));
        Assert.All(
            parsedPlan.DeliveryGroups.Where(g => g.Parent is not null),
            g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Parent!.Id), g.Parent));
    }

    [Fact]
    public void Parse_Plan_Preserves_ParentDependencies_On_Deferred_IncrementalPlan_Nodes()
    {
        // arrange
        // Same-subgraph hoist injects the key into the parent op so the
        // deferred incremental plan node carries a ParentStepRef on its plan step
        // and a {"parentNodeId": N} entry in its serialized dependencies.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var originalIncrementalPlanNode = originalPlan.IncrementalPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.ParentDependencies.Length > 0);
        var parsedIncrementalPlanNode = parsedPlan.IncrementalPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.Id == originalIncrementalPlanNode.Id);
        Assert.Equal(
            originalIncrementalPlanNode.ParentDependencies.ToArray(),
            parsedIncrementalPlanNode.ParentDependencies.ToArray());
        Assert.Equal(
            originalIncrementalPlanNode.Dependencies.Length,
            parsedIncrementalPlanNode.Dependencies.Length);
    }

    [Fact]
    public void Parse_Plan_Without_BatchingGroupId()
    {
        // arrange
        // Strip batchingGroupId from a formatted plan to simulate a legacy payload.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                    estimatedDelivery(postCode: "12345")
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        foreach (var node in json["nodes"]!.AsArray())
        {
            if (node?["type"]?.GetValue<string>() is "Operation")
            {
                node.AsObject().Remove("batchingGroupId");
            }
        }
        var legacyPlanSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(legacyPlanSource);

        // assert
        Assert.NotEmpty(parsedPlan.AllNodes.OfType<OperationExecutionNode>());
    }

    /// <summary>
    /// Composes a schema from two Apollo Federation subgraphs. Source schema
    /// "a" exposes the entity root fields and source schema "b" owns the
    /// entity name fields, so selecting a name plans an entity lookup that is
    /// routed to an Apollo execution node.
    /// </summary>
    private static FusionSchemaDefinition ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }

            type Query {
              products: [Product!]
              brands: [Brand!]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Brand

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        const string sourceSchemaB =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Brand @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Brand

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        var sourceTexts = new[]
        {
            new SourceSchemaText("a", sourceSchemaA),
            new SourceSchemaText("b", sourceSchemaB)
        };

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();

        foreach (var sourceText in sourceTexts)
        {
            composerOptions.SourceSchemas[sourceText.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        var composer = new SchemaComposer(sourceTexts, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}
