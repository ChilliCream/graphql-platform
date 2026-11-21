namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerContextTests : SourceSchemaMergerTestBase
{
    [Fact]
    public void Merge_Should_AddFusionContextAndOmitRequirements_When_RequireMapIsContextPathOnly()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    product: Product
                }

                type User @context(name: "userContext") {
                    id: ID!
                    userCurrency: Currency
                }

                type Currency {
                    isoCode: String!
                }

                type Product {
                    id: ID!
                    amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product @fusion__field(schema: A)
            }

            type Currency @fusion__type(schema: A) {
              isoCode: String! @fusion__field(schema: A)
            }

            type Product @fusion__type(schema: A) {
              amount: Int!
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  field: "amount(currency: String!): Int!"
                  map: ["$userContext.userCurrency.isoCode"]
                )
              id: ID! @fusion__field(schema: A)
            }

            type User
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "userContext") {
              id: ID! @fusion__field(schema: A)
              userCurrency: Currency @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_AddOnlyDeclaringTypeRequirements_When_RequireMapMixesContextPathAndField()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    product: Product
                }

                type User @context(name: "userContext") {
                    id: ID!
                    userCurrency: Currency
                }

                type Currency {
                    isoCode: String!
                }

                type Exchange {
                    rate: Float!
                }

                type Product {
                    id: ID!
                    exchange: Exchange
                    amount(
                        conversion: ConversionInput!
                            @require(
                                field: "{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }"
                            )
                    ): Int!
                }

                input ConversionInput {
                    currency: String!
                    rate: Float!
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product @fusion__field(schema: A)
            }

            type Currency @fusion__type(schema: A) {
              isoCode: String! @fusion__field(schema: A)
            }

            type Exchange @fusion__type(schema: A) {
              rate: Float! @fusion__field(schema: A)
            }

            type Product @fusion__type(schema: A) {
              amount: Int!
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  requirements: "exchange { rate }"
                  field: "amount(conversion: ConversionInput!): Int!"
                  map: [
                    "{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }"
                  ]
                )
              exchange: Exchange @fusion__field(schema: A)
              id: ID! @fusion__field(schema: A)
            }

            type User
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "userContext") {
              id: ID! @fusion__field(schema: A)
              userCurrency: Currency @fusion__field(schema: A)
            }

            input ConversionInput @fusion__type(schema: A) {
              currency: String! @fusion__inputField(schema: A)
              rate: Float! @fusion__inputField(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_AddFusionContextPerSourceSchema_When_ContextIsSetInMultipleSchemas()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    user: User
                }

                type User @context(name: "userContext") @context(name: "other") {
                    id: ID!
                }
                """,
                """
                # Schema B
                type User @context(name: "userContext") {
                    id: ID!
                    name: String
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              user: User @fusion__field(schema: A)
            }

            type User
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__context(schema: A, name: "userContext")
              @fusion__context(schema: A, name: "other")
              @fusion__context(schema: B, name: "userContext") {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              name: String @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_AddFusionContext_When_ContextIsSetOnInterfaceAndUnion()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    node: Node
                    result: SearchResult
                }

                interface Node @context(name: "nodeContext") {
                    id: ID!
                }

                type Photo implements Node {
                    id: ID!
                }

                union SearchResult @context(name: "resultContext") = Photo
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node: Node @fusion__field(schema: A)
              result: SearchResult @fusion__field(schema: A)
            }

            type Photo implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "nodeContext") {
              id: ID! @fusion__field(schema: A)
            }

            union SearchResult
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "resultContext")
              @fusion__unionMember(schema: A, member: "Photo") = Photo
            """);
    }
}
