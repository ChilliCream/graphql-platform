using System.Collections.Immutable;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class PolicyArtifactBinderTests : FusionTestBase
{
    [Fact]
    public void GetRequirements_Should_ConcatenateMemberRequirements_When_Batch()
    {
        // arrange
        // Nested-defer requirement routing can produce this batch shape. repo-n4r must pin that planner route when it lands.
        var first = CreateRequirement("first");
        var second = CreateRequirement("second");
        var third = CreateRequirement("third");
        var batch = new OperationBatchExecutionNode(
            1,
            [
                CreateOperation(2, [first, second]),
                CreateOperation(3, [third])
            ]);

        // act
        var requirements = PolicyArtifactBinder.GetBatchRequirements(batch.Operations.ToArray());

        // assert
        Assert.Equal([first, second, third], requirements);
    }

    [Fact]
    public void ProducesCandidate_Should_RequireCompiledSelection_When_ResultChildIsPruned()
    {
        // arrange
        var candidate = SelectionPath.Parse("$.product.rating");
        var prunedRoot = CreateOperation(
            id: 4,
            source: "query { product { id } }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.Create(Utf8GraphQLParser.Syntax.ParseSelectionSet("{ product }")));
        var lookup = CreateOperation(
            id: 5,
            source: "query { product { rating } }",
            target: SelectionPath.Parse("$.product"),
            sourcePath: SelectionPath.Parse("$.product"),
            ResultSelectionSet.Create(Utf8GraphQLParser.Syntax.ParseSelectionSet("{ rating }")));

        // act
        var produces = new[]
        {
            PolicyArtifactBinder.ProducesCandidate(prunedRoot, candidate),
            PolicyArtifactBinder.ProducesCandidate(lookup, candidate)
        };

        // assert
        Assert.Equal([false, true], produces);
    }

    [Fact]
    public void ValidatePolicyTopology_Should_RejectAmbiguousGuardedProducers_When_TargetHasNoCurrentOccurrences()
    {
        // arrange
        var first = CreateOperation(
            id: 6,
            source: "query { first }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ first }")));
        var second = CreateOperation(
            id: 7,
            source: "query { second }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ second }")));
        var target = new PolicyExecutionTarget
        {
            Occurrences =
            [
                new PolicyOccurrenceReference
                {
                    PlanPart = 1,
                    SelectionSetId = 1,
                    SelectionId = 1,
                    OccurrenceOrdinal = 0,
                    ApplicationOrdinal = 0,
                    Facet = PolicyOccurrenceFacet.ResidualEvaluation
                }
            ],
            Kind = PolicyTargetKind.Field,
            Path = SelectionPath.Parse("$.guarded"),
            TypeName = "Query",
            Policies = []
        };
        var policy = new PolicyExecutionNode(8, [target], []);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyArtifactBinder.ValidatePolicyTopology([first, second, policy], planPart: 0));

        // assert
        Assert.Equal(
            "A policy execution node has ambiguous guarded producers at the same target depth.",
            exception.Message);
    }

    [Theory]
    [InlineData(
        "EventStream",
        "Policies with requirements are not supported on subscription root fields; subscription policies must be requirement-free (evaluated per event).")]
    [InlineData("Node", "A policy execution node may only depend on operation nodes; node 9 is Node.")]
    [InlineData(
        "Introspection",
        "A policy execution node may only depend on operation nodes; node 9 is Introspection.")]
    public void ValidatePolicyTopology_Should_RejectNonOperationDependencies_When_PolicyHasNoProducer(
        string dependencyKind,
        string expectedMessage)
    {
        // arrange
        IOperationPlanNode dependency = dependencyKind switch
        {
            "EventStream" => new EventStreamExecutionNode(
                9,
                "onX",
                SelectionPath.Root,
                SelectionPath.Root,
                ResultSelectionSet.CreateFromPlan(
                    Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
                new EventStreamSource
                {
                    SchemaName = "a",
                    FieldName = "onX",
                    Topics = ["onX"],
                    Message = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")
                },
                "{ id }",
                []),
            "Node" => new NodeFieldExecutionNode(
                9,
                "node",
                new HotChocolate.Language.StringValueNode("account:1"),
                []),
            _ => new IntrospectionExecutionNode(
                9,
                [.. PlanOperation(CreateCompositeSchema(), "{ __typename }")
                    .AllNodes.OfType<IntrospectionExecutionNode>()
                    .Single()
                    .Selections],
                [])
        };
        var policy = new PolicyExecutionNode(
            10,
            [
                new PolicyExecutionTarget
                {
                    Occurrences =
                    [
                        new PolicyOccurrenceReference
                        {
                            PlanPart = 1,
                            SelectionSetId = 1,
                            SelectionId = 1,
                            OccurrenceOrdinal = 0,
                            ApplicationOrdinal = 0,
                            Facet = PolicyOccurrenceFacet.ResidualEvaluation
                        }
                    ],
                    Kind = PolicyTargetKind.Field,
                    Path = SelectionPath.Parse("$.guarded"),
                    TypeName = "Query",
                    Policies = []
                }
            ],
            []);
        policy.AddDependency(dependency);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyArtifactBinder.ValidatePolicyTopology([policy], planPart: 0));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    private static OperationRequirement CreateRequirement(string map)
        => new(
            "requirement",
            Utf8GraphQLParser.Syntax.ParseTypeReference("String"),
            SelectionPath.Parse("$.product"),
            new FieldSelectionMapParser(map).Parse());

    private static SingleOperationDefinition CreateOperation(
        int id,
        OperationRequirement[] requirements)
        => CreateOperation(
            id,
            "query { product { id } }",
            SelectionPath.Parse("$.product"),
            SelectionPath.Parse("$.product"),
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            requirements);

    private static OperationExecutionNode CreateOperation(
        int id,
        string source,
        SelectionPath target,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet)
    {
        var sourceText = Encoding.UTF8.GetBytes(source);

        return new OperationExecutionNode(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            lookupTypeName: null,
            schemaName: "a",
            target,
            sourcePath,
            requirements: [],
            forwardedVariables: [],
            resultSelectionSet,
            conditions: [],
            requiresFileUpload: false);
    }

    private static SingleOperationDefinition CreateOperation(
        int id,
        string source,
        SelectionPath target,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        OperationRequirement[] requirements)
    {
        var sourceText = Encoding.UTF8.GetBytes(source);

        return new SingleOperationDefinition(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            lookupTypeName: null,
            schemaName: "a",
            target,
            sourcePath,
            requirements,
            forwardedVariables: [],
            resultSelectionSet,
            conditions: [],
            requiresFileUpload: false);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_ResolveApolloEntityProvider_When_BodyUsesTypedFragment()
    {
        // arrange
        var schema = CreateMatrixApolloProviderSchema();
        var operation = PlanOperation(schema, "{ products { id } }").Operation;
        var sourceText = Encoding.UTF8.GetBytes(
            "query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Product { sku } } }");
        var provider = ApolloOperationExecutionNode.CreateFromParser(
            1,
            new OperationSourceText(
                "ApolloProduct",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            "Product",
            "b",
            SelectionPath.Parse("$.products"),
            requirements: [],
            forwardedVariables: [],
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ sku }")),
            conditions: [],
            requiresFileUpload: false,
            schema);
        provider.Seal();
        var policy = CreateMatrixPolicyNode([1], "{ sku }", "$.products.secured");
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
        Assert.False(hasGap);
        Assert.Equal((string.Empty, string.Empty), (coordinate, scope));
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_ResolveWrappedLookupProvider_When_LookupPathIsNotEmpty()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              products: [Product]
            }

            type Brand @key(fields: "id") {
              id: Int!
            }

            type Product @key(fields: "id") {
              id: Int!
              brand: Brand
            }
            """,
            """
            # name: b
            type Query {
              lookups: InternalLookups! @internal
            }

            type InternalLookups @internal {
              brandById(id: Int!): Brand! @lookup
            }

            type Brand @key(fields: "id") {
              id: Int!
              name: String!
            }
            """);
        var plan = PlanOperation(schema, "{ products { brand { name } } }");
        var provider = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => node.Source.ToString() == "$.lookups.brandById");
        var parentDependencies = provider.Dependencies.ToArray()
            .Select(dependency => dependency.Id)
            .Append(provider.Id)
            .Order()
            .ToArray();
        var policy = CreateMatrixPolicyNode(
            parentDependencies,
            requirement: "{ name }",
            path: "$.products.brand.secured");
        var incrementalPlan = new IncrementalPlan(
            plan.Operation,
            [policy],
            [policy],
            deliveryGroups: [],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            plan.AllNodes,
            out var coordinate,
            out var scope);

        // assert
        Assert.False(hasGap);
        Assert.Equal((string.Empty, string.Empty), (coordinate, scope));
    }

    [Fact]
    public void CreatePlan_Should_ResolvePolicyRequirementFromInternalLookup_When_PlanPartIsRoot()
    {
        // arrange
        var schema = CreateMatrixParentPolicySchema();

        // act
        var plan = PlanOperation(schema, "{ product(id: \"1\") { reviews } }");

        // assert
        var policy = Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>());
        var provider = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => node.SchemaName == "b");
        Assert.Contains(provider, policy.Dependencies.ToArray());
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_ResolveAllRequirementMapLeaves()
    {
        // arrange
        var operation = CreateMatrixOperation();
        var providers = new[]
        {
            CreateMatrixOperationNode(1, "query { product { profile { address { zip } } } }"),
            CreateMatrixOperationNode(2, "query { product { first } }"),
            CreateMatrixOperationNode(3, "query { product { second } }"),
            CreateMatrixOperationNode(4, "query { product { items { id } } }"),
            CreateMatrixOperationNode(5, "query { product { items { name } } }"),
            CreateMatrixOperationNode(6, "query { product { alpha } }"),
            CreateMatrixOperationNode(7, "query { product { beta } }"),
            CreateMatrixOperationNode(8, "query { product { media { ... on Book { isbn } } } }"),
            CreateMatrixOperationNode(9, "query { product { media { ... on Movie { title } } } }"),
            CreateMatrixOperationNode(10, "query { product { __fusion_sku: sku } }")
        };
        var guardedProducer = CreateMatrixOperationNode(
            11,
            "query { product { secured sku } }",
            [
                CreateMatrixRequirement("profile.address.zip"),
                CreateMatrixRequirement("{ first, second }"),
                CreateMatrixRequirement("items[{ id, name }]"),
                CreateMatrixRequirement("alpha | beta"),
                CreateMatrixRequirement("media<Book>.isbn | media<Movie>.title"),
                CreateMatrixRequirement("sku", internalAlias: "__fusion_sku")
            ]);
        var expectedProviderIds = GetMatrixProviderIds(
            ValueSelectionToSelectionSetRewriter.Rewrite(
                guardedProducer.GetRequirementsArray().Select(requirement => requirement.Map)));
        var expectedParentDependencies = expectedProviderIds
            .Append(guardedProducer.Id)
            .Order()
            .ToArray();
        var policy = CreateMatrixPolicyNode(expectedParentDependencies);
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
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), expectedProviderIds);
        Assert.False(hasGap);
        Assert.Equal((string.Empty, string.Empty), (coordinate, scope));
    }

    private static Operation CreateMatrixOperation()
    {
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              product: Product
            }

            type Product {
              secured: String
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

    private FusionSchemaDefinition CreateMatrixParentPolicySchema()
        => CreateMatrixSchema(
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
                """),
            new TestPolicy(
                "CanReadReviews",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ productSku }")));

    private static FusionSchemaDefinition CreateMatrixSchema(
        DocumentNode schemaDocument,
        params IPolicy[] policies)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(policies))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(schemaDocument, services);
    }

    private static FusionSchemaDefinition CreateMatrixApolloProviderSchema()
        => ComposeSchema(
            """
            # name: a
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              products: [Product!]!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type _Service { sdl: String! }
            union _Entity = Product
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """,
            """
            # name: b
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Product @key(fields: "id") {
              id: ID!
              sku: String!
            }

            type _Service { sdl: String! }
            union _Entity = Product
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """);

    private static OperationRequirement CreateMatrixRequirement(string map, string? internalAlias = null)
        => new(
            "requirement",
            Utf8GraphQLParser.Syntax.ParseTypeReference("String"),
            SelectionPath.Parse("$.product"),
            new FieldSelectionMapParser(map).Parse(),
            internalAlias);

    private static OperationExecutionNode CreateMatrixOperationNode(
        int id,
        string source,
        OperationRequirement[]? requirements = null)
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
            SelectionPath.Parse("$.product"),
            SelectionPath.Parse("$.product"),
            requirements ?? [],
            forwardedVariables: [],
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ secured }")),
            conditions: [],
            requiresFileUpload: false);

        node.Seal();
        return node;
    }

    private static PolicyExecutionNode CreateMatrixPolicyNode(
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

    private static int[] GetMatrixProviderIds(SelectionSetNode selectionSet)
    {
        var providers = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["profile.address.zip"] = 1,
            ["first"] = 2,
            ["second"] = 3,
            ["items.id"] = 4,
            ["items.name"] = 5,
            ["alpha"] = 6,
            ["beta"] = 7,
            ["media.isbn"] = 8,
            ["media.title"] = 9,
            ["sku"] = 10
        };
        var paths = new List<string>();
        AddLeafPaths(selectionSet, [], paths);
        return [.. paths.Select(path => providers[path]).Distinct().Order()];
    }

    private static void AddLeafPaths(
        SelectionSetNode selectionSet,
        List<string> segments,
        List<string> paths)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    segments.Add(field.Name.Value);
                    if (field.SelectionSet is null)
                    {
                        paths.Add(string.Join('.', segments));
                    }
                    else
                    {
                        AddLeafPaths(field.SelectionSet, segments, paths);
                    }

                    segments.RemoveAt(segments.Count - 1);
                    break;

                case InlineFragmentNode inlineFragment:
                    AddLeafPaths(inlineFragment.SelectionSet, segments, paths);
                    break;
            }
        }
    }
}
