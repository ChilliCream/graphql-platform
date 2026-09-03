using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementCrossEntityTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Resolve_ByNovice_When_Require_Crosses_Entity_Boundary()
    {
        // arrange
        var schema = CreateCircularCrossProviderSchema();

        // act
        // b.byNovice requires author.yearsOfExperience; author is owned by b but
        // yearsOfExperience is owned by a, so the requirement crosses an entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_ByExpert_When_Circular_Require_Chains_Through_ByNovice()
    {
        // arrange
        var schema = CreateCircularCrossProviderSchema();

        // act
        // a.byExpert requires b.byNovice, which itself requires a.author.yearsOfExperience,
        // chaining a circular requirement across the entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byExpert
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_ByNovice_When_Both_Providers_Have_Author_Lookup()
    {
        // arrange
        // mirror of the cross-provider topology an Apollo _entities gateway produces, where
        // BOTH subgraphs expose an authorById lookup (one per @key entity). The re-rooted
        // author lookup could otherwise satisfy its own key (author.id) through b's authorById,
        // which forms a lookup cycle; the planner must resolve author.id through the parent
        // path (postById { author { id } }) so the plan stays acyclic.
        var schema = CreateCircularCrossProviderMirrorSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Author_When_Require_Crosses_List_Entity_Boundary()
    {
        // arrange
        var schema = CreateWithArgumentCrossProviderSchema();

        // act
        // d.author requires comments[authorId]; comments is owned by d but authorId is
        // owned by c, so the list requirement crosses an entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                author {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Author_When_Require_Uses_Input_Object_List_Form()
    {
        // arrange
        // the real Apollo composition form: the requirement is an object-shorthand list
        // map over a list field owned by one subgraph whose leaf is owned by another.
        var schema = CreateWithArgumentInputObjectCrossProviderSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                author {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Keep_Requirement_And_Client_List_Field_Separate_When_Arguments_Differ()
    {
        // arrange
        // the client selects comments(limit: 1) while author @requires comments(limit: 3); the
        // requirement's list fetch must not adopt the client's argument (limit: 1). Before the
        // ReverseSelectionPath argument-safe fix the parent-path reconstruction resolved the
        // comments segment by response name only and reused the client field, collapsing the
        // requirement fetch onto limit: 1. The plan must keep both fetches with their own argument.
        var schema = CreateWithArgumentInputObjectCrossProviderSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                author {
                  id
                }
                comments(limit: 1) {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Subtotal_When_Require_Traverses_Connection_With_Nested_Require()
    {
        // arrange
        // cart owns items and subtotal; subtotal requires items(first: 50).nodes[product.discountedPrice].
        // discountedPrice is owned by promotions and itself requires price, which is owned by products.
        // The list element is not an entity, so the leaf is reached through the product lookup
        // spawned from the cart lookup that fetches the list; subtotal must depend on that lookup.
        var schema = CreateCartSchema(
            """
            items(first: Int, after: String): CartItemsConnection
            subtotal(lines: [Float!] @require(field: "items(first: 50).nodes[product.discountedPrice]")): Float!
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              viewer {
                cart {
                  subtotal
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Subtotal_When_Require_Traverses_Connection_With_Input_Object_List_Form()
    {
        // arrange
        // same topology, the requirement uses the input object list form.
        var schema = CreateCartSchema(
            """
            items(first: Int, after: String): CartItemsConnection
            subtotal(lines: [CartLineInput] @require(field: "items(first: 50).nodes[{ unitPrice: product.discountedPrice }]")): Float!
            """,
            cartTypes:
            """
            input CartLineInput {
              unitPrice: Float!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              viewer {
                cart {
                  subtotal
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Subtotal_When_Connection_Leaf_Has_No_Nested_Require()
    {
        // arrange
        // control: the leaf (price) is owned by products and has no requirement of its own.
        var schema = CreateCartSchema(
            """
            items(first: Int, after: String): CartItemsConnection
            subtotal(lines: [Float!] @require(field: "items(first: 50).nodes[product.price]")): Float!
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              viewer {
                cart {
                  subtotal
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Subtotal_When_Requiring_Field_Is_On_Other_Schema_Than_Connection()
    {
        // arrange
        // subtotal is owned by checkout while the items connection stays on cart, so the
        // requirement is inlined into the cart step and only the nested product leaf is
        // resolved by lookups spawned from that inlining.
        var schema = CreateCartSchema(
            """
            items(first: Int, after: String): CartItemsConnection
            """,
            additionalSchemas:
            """
            # name: checkout
            schema {
              query: Query
            }

            type Query {
              cartById(id: ID! @is(field: "id")): Cart @lookup @internal
            }

            type Cart @key(fields: "id") {
              id: ID!
              subtotal(lines: [Float!] @require(field: "items(first: 50).nodes[product.discountedPrice]")): Float!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              viewer {
                cart {
                  subtotal
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_ByNovice_When_Require_Is_Single_Provider()
    {
        // arrange
        // control: author and yearsOfExperience are both owned by a, so the requirement
        // stays within a single provider and plans today.
        var schema = CreateCircularSingleProviderSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_NeedsFlag_When_Require_Is_Same_Entity_Direct_Field()
    {
        // arrange
        // control: needsFlag requires flag on the same entity (Post) from another
        // provider, a direct scalar with no nested boundary, so it plans today.
        var schema = CreateSameEntityDirectRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                needsFlag
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateCircularCrossProviderSchema()
    {
        return ComposeSchema(
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
            }
            """);
    }

    private static FusionSchemaDefinition CreateCircularCrossProviderMirrorSchema()
    {
        return ComposeSchema(
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
            }
            """);
    }

    private static FusionSchemaDefinition CreateWithArgumentCrossProviderSchema()
    {
        return ComposeSchema(
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
              commentById(id: ID! @is(field: "id")): Comment @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
              body: String!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
              author(
                commentAuthorIds: [ID]
                  @require(field: "comments[authorId]")): Author
            }

            type Comment @key(fields: "id") {
              id: ID!
            }

            type Author {
              id: ID!
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateWithArgumentInputObjectCrossProviderSchema()
    {
        return ComposeSchema(
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
              commentById(id: ID! @is(field: "id")): Comment @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
              body: String!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
              author(
                comments: [CommentInput]
                  @require(field: "comments(limit: 3)[{ authorId: authorId }]")): Author
            }

            type Comment @key(fields: "id") {
              id: ID!
            }

            type Author {
              id: ID!
              name: String
            }

            input CommentInput {
              authorId: ID
            }
            """);
    }

    private static FusionSchemaDefinition CreateCircularSingleProviderSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author: Author!
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
            }

            type Post @key(fields: "id") {
              id: ID!
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }
            """);
    }

    private static FusionSchemaDefinition CreateSameEntityDirectRequireSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              needsFlag(flag: Boolean! @require(field: "flag")): Boolean!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              flag: Boolean!
            }
            """);
    }

    private static FusionSchemaDefinition CreateCartSchema(
        string cartFields,
        string cartTypes = "",
        params string[] additionalSchemas)
    {
        var cartSchema =
            $$"""
            # name: cart
            schema {
              query: Query
            }

            type Query {
              viewer: Viewer
              cartById(id: ID! @is(field: "id")): Cart @lookup @internal
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Viewer {
              cart: Cart
            }

            type Cart @key(fields: "id") {
              id: ID!
              {{cartFields}}
            }

            type CartItemsConnection {
              nodes: [CartItem!]
            }

            type CartItem {
              id: ID!
              quantity: Int!
              product: Product!
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            {{cartTypes}}
            """;

        const string productsSchema =
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float!
            }
            """;

        const string promotionsSchema =
            """
            # name: promotions
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              discountedPrice(price: Float! @require(field: "price")): Float!
            }
            """;

        return ComposeSchema([cartSchema, productsSchema, promotionsSchema, .. additionalSchemas]);
    }
}
