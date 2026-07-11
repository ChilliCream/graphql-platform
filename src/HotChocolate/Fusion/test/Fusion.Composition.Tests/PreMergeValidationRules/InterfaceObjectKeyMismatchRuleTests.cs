namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InterfaceObjectKeyMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InterfaceObjectKeyMismatchRule();

    // The stand-in keys on "sku", which is one of the interface's two keys, so this is valid even
    // though the stand-in does not repeat the "id" key.
    [Fact]
    public void Validate_StandInKeyMatchesInterfaceKey_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Media @key(fields: "id") @key(fields: "sku") {
                id: ID!
                sku: String!
                title: String!
            }
            """,
            """
            # Schema B
            type Media @interfaceObject @key(fields: "sku") {
                sku: String!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                rating: Int!
            }
            """
        ]);
    }

    // The stand-in keys on "upc", but the "Media" interface declares no key with that field.
    [Fact]
    public void Validate_StandInKeyNotOnInterface_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "upc") {
                    upc: String!
                    reviews: [Review!]!
                }

                type Review {
                    id: ID!
                    rating: Int!
                }
                """
            ],
            [
                """
                {
                    "message": "The @interfaceObject stand-in 'Media' in schema 'B' declares the key 'upc', which does not match any key declared on the interface it stands in for.",
                    "code": "INTERFACE_OBJECT_KEY_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Media",
                    "member": "Media",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // The "Media" interface declares no @key at all, so a stand-in can never key on it.
    [Fact]
    public void Validate_InterfaceHasNoKey_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Media {
                    id: ID!
                    title: String!
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    id: ID!
                    rating: Int!
                }
                """
            ],
            [
                """
                {
                    "message": "The @interfaceObject stand-in 'Media' in schema 'B' declares the key 'id', which does not match any key declared on the interface it stands in for.",
                    "code": "INTERFACE_OBJECT_KEY_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Media",
                    "member": "Media",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
