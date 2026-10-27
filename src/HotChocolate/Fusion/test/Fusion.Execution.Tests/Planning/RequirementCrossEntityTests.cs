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
        // c.byNovice requires author.yearsOfExperience; author is owned by b but
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
        // a.byExpert requires c.byNovice, which itself requires b.author with
        // a.yearsOfExperience, chaining a circular requirement across the entity boundary.
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
        // e.author requires comments[authorId]; comments is owned by d but authorId is
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
    public void Plan_Should_Resolve_Subtotal_When_Require_Traverses_Connection_With_Input_Object_List_Form()
    {
        // arrange
        // checkout owns subtotal, which requires items(first: 50).nodes[{ unitPrice: product.discountedPrice }]
        // from cart in the input object list form; discountedPrice itself requires price from products.
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
              subtotal(lines: [CartLineInput] @require(field: "items(first: 50).nodes[{ unitPrice: product.discountedPrice }]")): Float!
            }

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
        // control: checkout owns subtotal, and the leaf (price) is owned by products and has no
        // requirement of its own.
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
              subtotal(lines: [Float!] @require(field: "items(first: 50).nodes[product.price]")): Float!
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

    [Fact]
    public void Plan_Should_Resolve_Nested_Require_When_Requiring_Field_Is_Selected_Twice_Next_To_Required_Parent()
    {
        // arrange
        // unitPrice requires product.discountedPrice; product is owned by catalog and
        // discountedPrice by promotions, so the requirement crosses the entity boundary of product.
        // Selecting unitPrice under two response names next to a client selection of product must
        // plan like a single one.
        var schema = CreateNestedRequireSelectedTwiceSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              item {
                unitPrice
                u2: unitPrice
                product {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Nested_Require_When_Requiring_Field_Is_Selected_Twice_Next_To_Required_Parent_Under_Interface()
    {
        // arrange
        // same topology, but item is exposed as the Item interface so the requiring field
        // sits under an inline fragment and requirement inlining takes the ancestor path.
        var schema = CreateNestedRequireSelectedTwiceUnderInterfaceSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              item {
                ... on CartItem {
                  unitPrice
                  u2: unitPrice
                  product {
                    id
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Nested_Require_When_Requiring_Field_Is_Selected_Twice()
    {
        // arrange
        // same topology without the client selection of product. Both response names of
        // unitPrice must get their price from the promotions step.
        var schema = CreateNestedRequireSelectedTwiceSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              item {
                unitPrice
                u2: unitPrice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Reenter_Requiring_Schema_When_Requiring_Type_Has_Lookup()
    {
        // arrange
        // the CartItem key comes from cart itself, so once catalog has delivered product and
        // promotions its discountedPrice, the planner re-enters cart at CartItem through
        // cartItemById to pass the value.
        var schema = CreateCartPricingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              cart {
                items {
                  unitPrice
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Root_Name_When_Apollo_Requires_External_Self_Reference()
    {
        // arrange
        // a.name requires entity { age }, where entity and age are external in a and only b
        // resolves them, so b fetches the requirement and a's entity lookup receives it.
        var schema = CreateApolloSelfReferenceRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                name
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Nested_Name_When_Apollo_Requires_External_Self_Reference()
    {
        // arrange
        var schema = CreateApolloSelfReferenceRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                entity {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Deeply_Nested_Name_When_Apollo_Requires_External_Self_Reference()
    {
        // arrange
        var schema = CreateApolloSelfReferenceRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                entity {
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Batch_Name_Lookups_At_Every_Depth_When_Apollo_Requires_External_Self_Reference()
    {
        // arrange
        var schema = CreateApolloSelfReferenceRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                name
                entity {
                  name
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Root_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        // a.name requires entity.age, which only b resolves, so b fetches the requirement and
        // a's entityById lookup receives it.
        var schema = CreateSelfReferenceRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                name
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Nested_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        var schema = CreateSelfReferenceRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                entity {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Deeply_Nested_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        var schema = CreateSelfReferenceRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                entity {
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Batch_Name_Lookups_At_Every_Depth_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        var schema = CreateSelfReferenceRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              entity {
                name
                entity {
                  name
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Reenter_Requiring_Schema_When_Nested_Required_Field_Follows_Remote_First_Level()
    {
        // arrange
        // a.price requires customer.discount; customer is only served by b and discount by a, so
        // the requirement leaves a for customer and re-enters it through customerById.
        var schema = CreateNestedReentryRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              orders {
                id
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Reenter_Requiring_Schema_When_Client_Selects_Remote_First_Level_Next_To_Requiring_Field()
    {
        // arrange
        var schema = CreateNestedReentryRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              orders {
                id
                customer {
                  id
                }
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Reenter_Requiring_Schema_When_Abstract_First_Level_Has_No_Type_Condition()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              orderById(id: ID!): Order @lookup @internal
              customerById(id: ID!): Customer @lookup @internal
            }

            interface Person {
              id: ID!
              discount: Int
            }

            type Order {
              id: ID! @shareable
              price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer implements Person {
              id: ID! @shareable
              discount: Int
            }
            """,
            """
            # name: b
            type Query {
              orders: [Order]
            }

            interface Person {
              id: ID!
            }

            type Order {
              id: ID! @shareable
              customer: Person
            }

            type Customer implements Person {
              id: ID! @shareable
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              orders {
                id
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Nested_Required_Field_From_Third_Schema_When_Abstract_First_Level_Has_No_Type_Condition()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              orderById(id: ID!): Order @lookup @internal
            }

            type Order {
              id: ID! @shareable
              price(discount: Int @require(field: "customer.discount")): Int
            }
            """,
            """
            # name: b
            type Query {
              orders: [Order]
            }

            interface Person {
              id: ID!
            }

            type Order {
              id: ID! @shareable
              customer: Person
            }

            type Customer implements Person {
              id: ID! @shareable
            }
            """,
            """
            # name: c
            type Query {
              customerById(id: ID!): Customer @lookup @internal
            }

            interface Person {
              id: ID!
              discount: Int
            }

            type Customer implements Person {
              id: ID! @shareable
              discount: Int
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              orders {
                id
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateApolloSelfReferenceRequiresSchema()
    {
        return ComposeSchema(
            """
            # name: a
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.11"
                import: ["@external", "@key", "@shareable", "@requires"])

            type Query {
              entity: Entity! @shareable
            }

            type Entity @key(fields: "id") @shareable {
              id: ID
              age: Int! @external
              name: String! @requires(fields: "entity { age }")
              entity: Entity! @external
            }
            """,
            """
            # name: b
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.11"
                import: ["@external", "@key", "@shareable"])

            type Query {
              entity: Entity @shareable
            }

            type Entity @shareable {
              id: ID
              age: Int!
              entity: Entity!
            }
            """);
    }

    private static FusionSchemaDefinition CreateSelfReferenceRequireSchema()
    {
        return ComposeSchema(
            """
            # name: a
            type Query {
              entity: Entity! @shareable
              entityById(id: ID!): Entity @lookup @internal
            }

            type Entity @key(fields: "id") @shareable {
              id: ID
              name(entityAge: Int! @require(field: "entity.age")): String!
            }
            """,
            """
            # name: b
            type Query {
              entity: Entity @shareable
            }

            type Entity @shareable {
              id: ID
              age: Int!
              entity: Entity!
            }
            """);
    }

    private static FusionSchemaDefinition CreateNestedReentryRequireSchema()
    {
        return ComposeSchema(
            """
            # name: a
            type Query {
              orderById(id: ID!): Order @lookup @internal
              customerById(id: ID!): Customer @lookup @internal
            }

            type Order {
              id: ID! @shareable
              price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer {
              id: ID! @shareable
              discount: Int
            }
            """,
            """
            # name: b
            type Query {
              orders: [Order]
            }

            type Order {
              id: ID! @shareable
              customer: Customer
            }

            type Customer {
              id: ID! @shareable
            }
            """);
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
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
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
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
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
            }

            type Comment @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: e
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author(
                commentAuthorIds: [ID]
                  @require(field: "comments[authorId]")): Author
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
            }

            type Comment @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: e
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author(
                comments: [CommentInput]
                  @require(field: "comments(limit: 3)[{ authorId: authorId }]")): Author
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

    private static FusionSchemaDefinition CreateNestedRequireSelectedTwiceSchema()
    {
        return ComposeSchema(
            """
            # name: cart
            schema {
              query: Query
            }

            type Query {
              item: CartItem!
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            type CartItem @key(fields: "id") {
              id: ID!
              unitPrice(price: Float! @require(field: "product.discountedPrice")): Float!
            }
            """,
            """
            # name: catalog
            schema {
              query: Query
            }

            type Query {
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            type CartItem @key(fields: "id") {
              id: ID!
              product: Product!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
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
              discountedPrice: Float!
            }
            """);
    }

    private static FusionSchemaDefinition CreateCartPricingSchema()
    {
        return ComposeSchema(
            """
            # name: cart
            schema {
              query: Query
            }

            type Query {
              cart: Cart
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            type Cart {
              id: ID!
              items: [CartItem!]
            }

            type CartItem @key(fields: "id") {
              id: ID!
              unitPrice(price: Float! @require(field: "product.discountedPrice")): Float!
            }
            """,
            """
            # name: catalog
            schema {
              query: Query
            }

            type Query {
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            type CartItem @key(fields: "id") {
              id: ID!
              product: Product!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
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
              discountedPrice: Float!
            }
            """);
    }

    private static FusionSchemaDefinition CreateNestedRequireSelectedTwiceUnderInterfaceSchema()
    {
        return ComposeSchema(
            """
            # name: cart
            schema {
              query: Query
            }

            type Query {
              item: Item!
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            interface Item {
              id: ID!
            }

            type CartItem implements Item @key(fields: "id") {
              id: ID!
              unitPrice(price: Float! @require(field: "product.discountedPrice")): Float!
            }
            """,
            """
            # name: catalog
            schema {
              query: Query
            }

            type Query {
              cartItemById(id: ID! @is(field: "id")): CartItem @lookup @internal
            }

            type CartItem @key(fields: "id") {
              id: ID!
              product: Product!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
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
              discountedPrice: Float!
            }
            """);
    }
}
