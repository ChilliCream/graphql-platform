using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class AbstractLookupFanoutPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Keep_SiblingBranch_Clean_When_Duplicate_TypeFragments_Present()
    {
        // arrange
        // Two `... on Book` fragments on products must not root the Book key requirement into the
        // sibling Magazine-targeted reviews lookup, which would emit an invalid cross-type fragment.
        var schema = CreateProductTitleSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Book { sku }
                ... on Magazine { sku }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Prune_Impossible_Concrete_Fragment_When_Parent_Is_Object()
    {
        // arrange
        // The nested Product selection is partitioned into concrete Book and Magazine branches.
        // A Magazine-only title lookup must not add its id requirement to the sibling Book lookup.
        var schema = CreateNestedProductReviewSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                reviews {
                  product {
                    sku
                    ... on Magazine { title }
                    ... on Book { reviewsCount }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Prune_NonNode_Implementor_When_InterfaceFragment_Is_In_NodeSelectionSet()
    {
        // arrange
        // The Manageable fragment fans out over all Manageable implementors, but the enclosing
        // node selection set only ever yields Node implementors, so Draft must not get a branch.
        var schema = CreateManageableNodeSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($id: ID!) {
              node(id: $id) {
                __typename
                ... on Node { id }
                ... on Manageable { canEdit }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Prune_NonNode_Implementor_When_NodeSelectionSet_Has_No_Shared_Selections()
    {
        // arrange
        var schema = CreateManageableNodeSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($id: ID!) {
              node(id: $id) {
                __typename
                ... on Manageable { canEdit }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Merge_Nested_Lookups_Across_Union_Members_When_Sibling_Asset_Selections_Differ()
    {
        // arrange
        // Four union members select the same asset field: two identical, one with the entity
        // fields reordered and one with an extra field. The nested meterType and category
        // lookups must each merge into one multi-target operation that covers all four member
        // paths and fans in on every sibling asset lookup.
        var schema = CreateUnionSiblingAssetSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category { allowedMeterTypes { type } }
                    meterType { type }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType { type }
                    statusMessage
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Attribute_Nested_Apollo_Lookup_Dependencies_Per_Sibling_When_Sibling_Asset_Selections_Differ()
    {
        // arrange
        // The Apollo Federation twin of the union sibling scenario: Apollo entity lookups never
        // merge into multi-target operations, so each nested MeterType and Category lookup must
        // depend only on the sibling asset lookup that feeds its own target path.
        var schema = CreateApolloUnionSiblingAssetSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category { allowedMeterTypes { type } }
                    meterType { type }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType { type }
                    statusMessage
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Attribute_Nested_Apollo_Lookup_Dependencies_Per_Sibling_Without_Request_Grouping()
    {
        // arrange
        // Without request grouping every Apollo entity lookup stays an individual execution
        // node, so the node-level dependency wiring must also keep each nested MeterType and
        // Category lookup tied to the sibling asset lookup that feeds its own target path.
        var schema = CreateApolloUnionSiblingAssetSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category { allowedMeterTypes { type } }
                    meterType { type }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType { type }
                    statusMessage
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
              }
            }
            """,
            new OperationPlannerOptions
            {
                EnableRequestGrouping = false
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_Assign_Unique_Ids_Across_Nodes_And_Definitions(bool enableRequestGrouping)
    {
        // arrange
        // Batch wrapper nodes receive fresh identifiers, so no identifier may
        // denote both an execution node and an operation definition.
        var schema = CreateUnionSiblingAssetSchema();
        var apolloSchema = CreateApolloUnionSiblingAssetSchema();
        var options = new OperationPlannerOptions { EnableRequestGrouping = enableRequestGrouping };

        // act
        var plan = PlanOperation(schema, UnionSiblingAssetOperation, options);
        var apolloPlan = PlanOperation(apolloSchema, UnionSiblingAssetOperation, options);

        // assert
        Assert.Distinct(CollectNodeAndDefinitionIds(plan));
        Assert.Distinct(CollectNodeAndDefinitionIds(apolloPlan));
    }

    private const string UnionSiblingAssetOperation =
        """
        query testQuery {
          search {
            ... on ExistingResult {
              asset {
                meterType { type }
                category { allowedMeterTypes { type } }
              }
            }
            ... on ConfigMatchesResult {
              asset {
                category { allowedMeterTypes { type } }
                meterType { type }
              }
            }
            ... on NotSubmissableResult {
              asset {
                meterType { type }
                statusMessage
                category { allowedMeterTypes { type } }
              }
            }
            ... on CustomResult {
              asset {
                meterType { type }
                category { allowedMeterTypes { type } }
              }
            }
          }
        }
        """;

    private static List<int> CollectNodeAndDefinitionIds(OperationPlan plan)
    {
        var ids = new List<int>();

        foreach (var node in plan.AllNodes)
        {
            ids.Add(node.Id);

            switch (node)
            {
                case OperationBatchExecutionNode batchNode:
                    foreach (var operation in batchNode.Operations)
                    {
                        ids.Add(operation.Id);
                    }
                    break;

                case ApolloOperationBatchExecutionNode apolloBatchNode:
                    foreach (var operation in apolloBatchNode.Operations)
                    {
                        ids.Add(operation.Id);
                    }
                    break;
            }
        }

        return ids;
    }

    // sku is co-located with the products root in "a", reviews in "r", Book-only title in "books".
    private static FusionSchemaDefinition CreateProductTitleSchema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: r
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] }
            type Book implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Magazine implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Review { id: ID! }
            """,
            """
            # name: books
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
            }

            type Book @key(fields: "id") { id: ID! title: String }
            """);

    private static FusionSchemaDefinition CreateNestedProductReviewSchema()
        => ComposeSchema(
            """
            # name: products
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: reviews
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] reviewsCount: Int }
            type Book implements Product @key(fields: "id") {
              id: ID!
              reviews: [Review]
              reviewsCount: Int
            }
            type Magazine implements Product @key(fields: "id") {
              id: ID!
              reviews: [Review]
              reviewsCount: Int
            }
            type Review { product: Product }
            """,
            """
            # name: magazines
            schema { query: Query }

            type Query {
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            type Magazine @key(fields: "id") { id: ID! title: String }
            """);

    // Ticket is both Manageable and a Node, Draft is only Manageable and has no id.
    private static FusionSchemaDefinition CreateManageableNodeSchema()
        => ComposeSchema(
            """
            # name: tickets
            schema { query: Query }

            type Query {
              node(id: ID!): Node @lookup
              draft: Draft
            }

            interface Node { id: ID! }
            interface Manageable { canEdit: Boolean! }

            type Ticket implements Node & Manageable { id: ID! canEdit: Boolean! }
            type Draft implements Manageable { canEdit: Boolean! name: String }
            """);

    // The union and the Asset key live in "a", the Asset entity fields in "b". The meterType
    // and category stubs in "b" resolve their full fields through internal lookups in "a".
    private static FusionSchemaDefinition CreateUnionSiblingAssetSchema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              search: [CandidateResult!]!
              meterTypeById(id: Int!): MeterType @lookup @internal
              categoryByDbId(dbId: Int!): Category @lookup @internal
            }

            union CandidateResult =
              | NotSubmissableResult
              | ExistingResult
              | ConfigMatchesResult
              | CustomResult

            type ExistingResult { asset: Asset }
            type ConfigMatchesResult { asset: Asset }
            type NotSubmissableResult { asset: Asset }
            type CustomResult { asset: Asset }

            type Asset @key(fields: "dbId") { dbId: Int! }

            type MeterType @key(fields: "id") {
              id: Int!
              type: String!
            }

            type Category @key(fields: "dbId") {
              dbId: Int!
              allowedMeterTypes: [MeterType!]!
            }
            """,
            """
            # name: b
            schema { query: Query }

            type Query {
              assetByDbId(dbId: Int!): Asset @lookup @internal
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
              statusMessage: String
              meterType: MeterType
              category: Category
            }

            type MeterType @key(fields: "id") { id: Int! }

            type Category @key(fields: "dbId") { dbId: Int! }
            """);

    // The Apollo Federation variant: the union and the MeterType and Category entities live in
    // "search", the Asset entity fields in "assets". All entity traversal happens through
    // '_entities' lookups.
    private static FusionSchemaDefinition CreateApolloUnionSiblingAssetSchema()
        => ComposeSchema(
            """
            # name: search
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              search: [CandidateResult!]!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            union CandidateResult =
              | NotSubmissableResult
              | ExistingResult
              | ConfigMatchesResult
              | CustomResult

            type ExistingResult { asset: Asset }
            type ConfigMatchesResult { asset: Asset }
            type NotSubmissableResult { asset: Asset }
            type CustomResult { asset: Asset }

            type Asset @key(fields: "id", resolvable: false) { id: String! }

            type MeterType @key(fields: "id") {
              id: String!
              type: String!
            }

            type Category @key(fields: "id") {
              id: String!
              allowedMeterTypes: [MeterType!]!
            }

            type _Service { sdl: String! }

            union _Entity = MeterType | Category

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """,
            """
            # name: assets
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Asset @key(fields: "id") {
              id: String!
              statusMessage: String
              meterType: MeterType
              category: Category
            }

            type MeterType @key(fields: "id", resolvable: false) { id: String! }

            type Category @key(fields: "id", resolvable: false) { id: String! }

            type _Service { sdl: String! }

            union _Entity = Asset

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """);
}
