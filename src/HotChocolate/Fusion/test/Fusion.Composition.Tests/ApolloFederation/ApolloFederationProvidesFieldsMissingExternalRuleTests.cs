using HotChocolate.Fusion.SourceSchemaValidationRules;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ApolloFederationProvidesFieldsMissingExternalRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesFieldsMissingExternalRule();

    [Fact]
    public void Validate_Should_Succeed_When_LocalFieldTraversesIntoAbstractProvidedLeaf()
    {
        AssertValid(
        [
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@external", "@provides"])

            type Query {
                book: Book @provides(fields: "animals { ... on Dog { name } }")
            }

            type Book {
                animals: [Animal]
            }

            interface Animal {
                id: ID!
            }

            type Dog implements Animal {
                id: ID!
                name: String @external
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_InterfacePathHasExternalRuntimeField()
    {
        AssertValid(
        [
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@external", "@provides"])

            type Query {
                media: Media @provides(fields: "animals { id name }")
            }

            interface Media {
                animals: [Animal]
            }

            type Book implements Media {
                animals: [Animal] @external
            }

            interface Animal {
                id: ID!
                name: String
            }

            type Dog implements Animal {
                id: ID! @external
                name: String @external
            }

            type Cat implements Animal {
                id: ID! @external
                name: String @external
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_NestedLeafHasNoExternalPath()
    {
        AssertInvalid(
            [
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@provides"])

                type Query {
                    order: Order
                }

                type Order {
                    buyer: User @provides(fields: "info { address }")
                }

                type User {
                    info: UserInfo
                }

                type UserInfo {
                    address: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'Order.buyer' in schema 'A' references field 'UserInfo.address', which must be marked as external.",
                    "code": "PROVIDES_FIELDS_MISSING_EXTERNAL",
                    "severity": "Error",
                    "coordinate": "Order.buyer",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
