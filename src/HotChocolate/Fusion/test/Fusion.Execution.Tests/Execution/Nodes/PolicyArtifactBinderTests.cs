using System.Text;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class PolicyArtifactBinderTests : FusionTestBase
{
    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_ResolveEveryRequirementMapLeaf()
    {
        // arrange
        var operation = CreateOperation();
        var providers = new[]
        {
            CreateOperationNode(1, "query { product { profile { address { zip } } } }"),
            CreateOperationNode(2, "query { product { first } }"),
            CreateOperationNode(3, "query { product { second } }"),
            CreateOperationNode(4, "query { product { items { id } } }"),
            CreateOperationNode(5, "query { product { items { name } } }"),
            CreateOperationNode(6, "query { product { alpha } }"),
            CreateOperationNode(7, "query { product { beta } }"),
            CreateOperationNode(8, "query { product { media { ... on Book { isbn } } } }"),
            CreateOperationNode(9, "query { product { media { ... on Movie { title } } } }"),
            CreateOperationNode(10, "query { product { __fusion_sku: sku } }")
        };
        var guardedProducer = CreateOperationNode(
            11,
            "query { product { secured sku } }",
            [
                CreateRequirement("profile.address.zip"),
                CreateRequirement("{ first, second }"),
                CreateRequirement("items[{ id, name }]"),
                CreateRequirement("alpha | beta"),
                CreateRequirement("media<Book>.isbn | media<Movie>.title"),
                CreateRequirement("sku", internalAlias: "__fusion_sku")
            ]);
        var policy = CreatePolicyNode(parentDependencies: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [.. providers, guardedProducer],
            out var coordinate,
            out var scope);

        // assert
        Assert.False(hasGap);
        Assert.Equal(string.Empty, coordinate);
        Assert.Equal(string.Empty, scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_RejectCoordinatedProviderAndPolicyParentMutation()
    {
        // arrange
        var operation = CreateOperation();
        var originalProvider = CreateOperationNode(1, "query { product { id } }");
        var unrelatedNode = CreateOperationNode(3, "query { product { unrelated } }");
        var producer = CreateOperationNode(
            2,
            "query { product { sku } }",
            [CreateRequirement("id")],
            dependencies: [unrelatedNode]);
        var policy = CreatePolicyNode(parentDependencies: [2, 3]);
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [originalProvider, producer, unrelatedNode],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.secured", coordinate);
        Assert.Equal("nested defer", scope);
    }

    [Fact]
    public void JsonParser_Should_RejectCoordinatedProviderAndPolicyParentMutation()
    {
        // arrange
        var schema = CreateParentAuthoritySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              unrelated
              product(id: "1") {
                name
                ... @defer {
                  reviews
                }
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);
        var policy = json["incrementalPlans"]!.AsArray()[0]!["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var parentDependencies = policy["dependencies"]!.AsArray()
            .OfType<JsonObject>()
            .ToArray();
        var providerId = parentDependencies.Max(node => node["parentNodeId"]!.GetValue<int>());
        var unrelatedNode = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "d");
        var unrelatedId = unrelatedNode["id"]!.GetValue<int>();
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<int>() == providerId);
        provider["dependencies"] = new JsonArray(unrelatedId);
        var parentProviderToReplace = parentDependencies.MinBy(
            node => node["parentNodeId"]!.GetValue<int>())!;
        policy["dependencies"]!.AsArray()[policy["dependencies"]!.AsArray().IndexOf(parentProviderToReplace)] =
            new JsonObject { ["parentNodeId"] = unrelatedId };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node parent dependencies must exactly match its parent requirement providers.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectParentProviderThatSelectsItsOwnRequirement()
    {
        // arrange
        var schema = CreateParentAuthoritySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              unrelated
              product(id: "1") {
                name
                ... @defer {
                  reviews
                }
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        provider["requirements"]!.AsArray()[0]!.AsObject()["selectionMap"] = "productSku";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy parent requirement provider cannot be resolved from the immediate parent scope.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectMutuallyCircularParentProviders()
    {
        // arrange
        var schema = CreateParentAuthoritySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              unrelated
              product(id: "1") {
                name
                ... @defer {
                  reviews
                }
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "a");
        provider["requirements"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "__fusion_cycle_productSku",
                ["type"] = "String!",
                ["path"] = "$.product",
                ["selectionMap"] = "productSku"
            }
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy parent requirement provider cannot satisfy its own pre-execution requirements.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectWrongSiblingRequirementProvider()
    {
        // arrange
        var schema = CreateParentAuthoritySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              unrelated
              product(id: "1") {
                name
                ... @defer {
                  reviews
                }
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        provider["operation"]!.AsObject()["document"] =
            "query Op_defer_1($__fusion_1_id: ID!) { productById(id: $__fusion_1_id) { id } }";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy parent requirement provider cannot be resolved from the immediate parent scope.",
            exception.Message);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_RejectWrongSiblingRequirementLeaf()
    {
        // arrange
        var operation = CreateOperation();
        var provider = CreateOperationNode(1, "query { product { other } }");
        var policy = CreatePolicyNode([1], "{ expected }");
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [provider],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.secured", coordinate);
        Assert.Equal("nested defer", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_RejectMovedNestedTypeConditions()
    {
        // arrange
        var operation = CreateOperation();
        var provider = CreateOperationNode(
            1,
            "query { product { ... on Movie { media { ... on Book { id } } } } }");
        var policy = CreatePolicyNode(
            [1],
            "{ ... on Book { media { ... on Movie { id } } } }");
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [provider],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.secured", coordinate);
        Assert.Equal("nested defer", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_RejectMismatchedBasePathTypeCondition()
    {
        // arrange
        var operation = CreateOperation();
        var provider = CreateOperationNode(
            1,
            "query { product { ... on Movie { id } } }",
            target: "$.product<Movie>",
            sourcePath: "$.product<Movie>");
        var policy = CreatePolicyNode(
            [1],
            "{ ... on Book { id } }",
            path: "$.product<Book>.secured");
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [provider],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.secured", coordinate);
        Assert.Equal("nested defer", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_AcceptConcreteProviderForAbstractRequirement()
    {
        // arrange
        var operation = CreateOperation();
        var concreteProvider = CreateOperationNode(
            1,
            "query { product { media { ... on Book { id } } } }");
        var guardedProducer = CreateOperationNode(
            2,
            "query { product { secured } }",
            [CreateRequirement("media<Media>.id")]);
        var policy = CreatePolicyNode([1, 2], "{ secured }");
        var incrementalPlan = new IncrementalPlan(
            operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            [concreteProvider, guardedProducer],
            out var coordinate,
            out var scope);

        // assert
        Assert.False(hasGap);
        Assert.Equal(string.Empty, coordinate);
        Assert.Equal(string.Empty, scope);
    }

    [Fact]
    public void JsonParser_Should_AcceptCompatibleExplicitSourceInlineFragment()
    {
        // arrange
        var schema = CreateNestedConditionSchema();
        var plan = PlanOperation(
            schema,
            "{ otherInterface { next { ... on Author { secured } } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        provider["target"] = "$.otherInterface.next";
        provider["source"] = "$.authorById<Author>";

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        Assert.Single(parsedPlan.AllNodes.OfType<PolicyExecutionNode>());
    }

    [Fact]
    public void JsonParser_Should_RejectIncompatibleExplicitSourceInlineFragment()
    {
        // arrange
        var schema = CreateNestedConditionSchema();
        var plan = PlanOperation(
            schema,
            "{ otherInterface { next { ... on Author { secured } } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        provider["source"] = "$.authorById<Editor>";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node dependencies must exactly match its producer and requirement providers.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectMismatchedBaseSelectionPathInlineFragmentCondition()
    {
        // arrange
        var schema = CreateNestedConditionSchema();
        var plan = PlanOperation(
            schema,
            "{ otherInterface { next { ... on Author { secured } } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        provider["target"] = "$.otherInterface.next<Editor>";
        provider["source"] = "$.authorById<Author>";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node dependencies must exactly match its producer and requirement providers.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectMovedNestedInlineFragmentCondition()
    {
        // arrange
        var schema = CreateNestedConditionSchema();
        var plan = PlanOperation(
            schema,
            "{ otherInterface { next { ... on Author { secured } } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        Assert.Equal("$.otherInterface.next<Author>", provider["target"]!.GetValue<string>());
        provider["target"] = "$.otherInterface<Author>.next";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node dependencies must exactly match its producer and requirement providers.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectSwappedNestedInlineFragmentConditions()
    {
        // arrange
        var schema = CreateNestedConditionSchema();
        var plan = PlanOperation(
            schema,
            "{ otherInterface { ... on Author { next { ... on Editor { secured } } } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["schema"]?.GetValue<string>() == "b");
        Assert.Equal("$.otherInterface<Author>.next<Editor>", provider["target"]!.GetValue<string>());
        provider["target"] = "$.otherInterface<Editor>.next<Author>";

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node dependencies must exactly match its producer and requirement providers.",
            exception.Message);
    }

    private static Operation CreateOperation()
    {
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              product: Product
            }

            type Product {
              secured: String
              expected: String
              other: String
              id: ID!
              profile: Profile
              first: String
              second: String
              items: [Item!]
              alpha: String
              beta: String
              sku: String
              media: Media
            }

            type Profile {
              address: Address
            }

            type Address {
              zip: String
            }

            type Item {
              id: ID!
              name: String
            }

            interface Media {
              id: ID!
            }

            type Book implements Media {
              id: ID!
              isbn: String
            }

            type Movie implements Media {
              id: ID!
              title: String
            }
            """);
        return PlanOperation(schema, "{ product { secured } }").Operation;
    }

    private static FusionSchemaDefinition CreateNestedConditionSchema()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy(
                        "CanReadSecured",
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ age }"))))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  otherInterface: OtherInterface
                }

                interface OtherInterface {
                  id: ID!
                  next: OtherInterface
                }

                type Author implements OtherInterface @key(fields: "id") {
                  id: ID!
                  next: OtherInterface
                }

                type Editor implements OtherInterface @key(fields: "id") {
                  id: ID!
                  next: OtherInterface
                }
                """,
                """
                # name: b
                type Query {
                  authorById(id: ID!): Author @lookup @internal
                  editorById(id: ID!): Editor @lookup @internal
                }

                interface OtherInterface {
                  id: ID!
                }

                type Author implements OtherInterface @key(fields: "id") {
                  id: ID!
                  age: Int!
                }

                type Editor implements OtherInterface @key(fields: "id") {
                  id: ID!
                  age: Int!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  authorById(id: ID!): Author @lookup @internal
                  editorById(id: ID!): Editor @lookup @internal
                }

                interface OtherInterface {
                  id: ID!
                }

                type Author implements OtherInterface @key(fields: "id") {
                  id: ID!
                  secured: String @policy(names: "CanReadSecured", onDenied: NULL)
                }

                type Editor implements OtherInterface @key(fields: "id") {
                  id: ID!
                  secured: String @policy(names: "CanReadSecured", onDenied: NULL)
                }
                """),
            services);
    }

    private static FusionSchemaDefinition CreateParentAuthoritySchema()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy(
                        "CanReadReviews",
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ productSku }"))))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product(id: ID!): Product @lookup
                }

                type Product @key(fields: "id") {
                  id: ID!
                  name: String!
                }
                """,
                """
                # name: b
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  productSku: String!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  reviews(productSku: String! @require(field: "productSku")): [String!]!
                    @policy(names: "CanReadReviews", onDenied: NULL)
                }
                """,
                """
                # name: d
                type Query {
                  unrelated: String!
                }
                """),
            services);
    }

    private static (JsonObject Json, JsonOperationPlanParser Parser) SerializePlan(
        FusionSchemaDefinition schema,
        OperationPlan plan)
    {
        using var buffer = new PooledArrayWriter();
        new JsonOperationPlanFormatter().Format(buffer, plan);
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        return (JsonNode.Parse(buffer.WrittenSpan)!.AsObject(), new JsonOperationPlanParser(compiler));
    }

    private static OperationRequirement CreateRequirement(string map, string? internalAlias = null)
        => new(
            "requirement",
            Utf8GraphQLParser.Syntax.ParseTypeReference("String"),
            SelectionPath.Parse("$.product"),
            new FieldSelectionMapParser(map).Parse(),
            internalAlias);

    private static OperationExecutionNode CreateOperationNode(
        int id,
        string source,
        OperationRequirement[]? requirements = null,
        IOperationPlanNode[]? dependencies = null,
        string target = "$.product",
        string sourcePath = "$.product")
    {
        var sourceText = Encoding.UTF8.GetBytes(source);
        var node = new OperationExecutionNode(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            lookupTypeName: null,
            schemaName: "a",
            SelectionPath.Parse(target),
            SelectionPath.Parse(sourcePath),
            requirements ?? [],
            forwardedVariables: [],
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ secured }")),
            conditions: [],
            requiresFileUpload: false);

        if (dependencies is not null)
        {
            foreach (var dependency in dependencies)
            {
                node.AddDependency(dependency);
            }
        }

        node.Seal();
        return node;
    }

    private static PolicyExecutionNode CreatePolicyNode(
        int[] parentDependencies,
        string requirement = "{ sku }",
        string path = "$.product.secured")
    {
        var policy = new PolicyExecutionNode(
            12,
            [
                new PolicyExecutionTarget
                {
                    Kind = PolicyTargetKind.Field,
                    Path = SelectionPath.Parse(path),
                    TypeName = "Product",
                    Policies = [],
                    Requirements =
                    [
                        new PolicyRequirement
                        {
                            PolicyName = "CanReadSecured",
                            SelectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirement)
                        }
                    ]
                }
            ],
            conditions: []);

        foreach (var parentDependency in parentDependencies)
        {
            policy.AddParentDependency(parentDependency);
        }

        policy.Seal();
        return policy;
    }
}
